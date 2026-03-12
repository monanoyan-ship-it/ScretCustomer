using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _context;

    public AssessmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== Hiyerarşi Yönetimi =====

    public async Task<List<AssessmentParticipant>> GetParticipantsAsync(int assignmentPeriodId)
    {
        return await _context.AssessmentParticipants
            .Include(x => x.CustomerPersonnel)
            .Include(x => x.Children)
                .ThenInclude(c => c.CustomerPersonnel)
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    public async Task<AssessmentParticipant> AddParticipantAsync(int assignmentPeriodId, int customerPersonnelId, int? parentId)
    {
        // Aynı dönemde aynı kişi olmamalı
        var exists = await _context.AssessmentParticipants
            .AnyAsync(x => x.AssignmentPeriodId == assignmentPeriodId
                        && x.CustomerPersonnelId == customerPersonnelId);
        if (exists)
            throw new InvalidOperationException("Bu kişi zaten bu dönemde katılımcı olarak eklenmiş.");

        // ParentId döngüsel olmamalı (parent da aynı döneme ait olmalı)
        if (parentId.HasValue)
        {
            var parent = await _context.AssessmentParticipants
                .FirstOrDefaultAsync(x => x.Id == parentId.Value
                                       && x.AssignmentPeriodId == assignmentPeriodId);
            if (parent == null)
                throw new InvalidOperationException("Belirtilen üst katılımcı bu dönemde bulunamadı.");
        }

        // Sıra numarası: aynı parent altındaki son sıra + 1
        var maxOrder = await _context.AssessmentParticipants
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId && x.ParentId == parentId)
            .MaxAsync(x => (int?)x.Order) ?? 0;

        var participant = new AssessmentParticipant
        {
            AssignmentPeriodId = assignmentPeriodId,
            CustomerPersonnelId = customerPersonnelId,
            ParentId = parentId,
            Order = maxOrder + 1
        };

        _context.AssessmentParticipants.Add(participant);
        await _context.SaveChangesAsync();

        return participant;
    }

    public async Task RemoveParticipantAsync(int participantId)
    {
        var participant = await _context.AssessmentParticipants
            .FirstOrDefaultAsync(x => x.Id == participantId);

        if (participant == null) return;

        // Alt katılımcıları da sil (recursive soft delete)
        await RemoveChildrenRecursiveAsync(participantId);

        participant.IsDeleted = true;
        await _context.SaveChangesAsync();
    }

    public async Task MoveParticipantAsync(int participantId, int? newParentId)
    {
        var participant = await _context.AssessmentParticipants
            .FirstOrDefaultAsync(x => x.Id == participantId);

        if (participant == null)
            throw new InvalidOperationException("Katılımcı bulunamadı.");

        // Döngüsel ilişki kontrolü: newParentId, participantId'nin alt ağacında olmamalı
        if (newParentId.HasValue)
        {
            var isDescendant = await IsDescendantAsync(newParentId.Value, participantId);
            if (isDescendant)
                throw new InvalidOperationException("Döngüsel hiyerarşi oluşturulamaz.");

            var parent = await _context.AssessmentParticipants
                .FirstOrDefaultAsync(x => x.Id == newParentId.Value
                                       && x.AssignmentPeriodId == participant.AssignmentPeriodId);
            if (parent == null)
                throw new InvalidOperationException("Belirtilen üst katılımcı bu dönemde bulunamadı.");
        }

        participant.ParentId = newParentId;
        await _context.SaveChangesAsync();
    }

    public async Task<int> ImportFromOrganizationAsync(int assignmentPeriodId, int projectId)
    {
        // Projenin müşterisini bul
        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);
        if (project?.CustomerId == null)
            throw new InvalidOperationException("Projenin müşterisi bulunamadı.");

        var customerId = project.CustomerId.Value;

        // Müşterinin organizasyon yapısını al (CustomerOrganization ağacı)
        var organizations = await _context.CustomerOrganizations
            .Where(x => x.CustomerId == customerId && x.IsActive)
            .OrderBy(x => x.Level)
            .ThenBy(x => x.Order)
            .ToListAsync();

        // Personel-organizasyon atamalarını al (SupervisorId ile)
        var personnelOrgs = await _context.CustomerPersonnelOrganizations
            .Include(x => x.CustomerPersonnel)
            .Where(x => organizations.Select(o => o.Id).Contains(x.CustomerOrganizationId)
                      && x.CustomerPersonnel.IsActive)
            .ToListAsync();

        if (!personnelOrgs.Any()) return 0;

        // Mevcut dönem katılımcılarını temizle (varsa)
        var existingParticipants = await _context.AssessmentParticipants
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId)
            .ToListAsync();
        foreach (var ep in existingParticipants)
            ep.IsDeleted = true;

        // SupervisorId ilişkisinden ağaç oluştur
        // Önce tüm benzersiz personelleri topla
        var personnelIds = personnelOrgs.Select(x => x.CustomerPersonnelId).Distinct().ToList();
        var supervisorMap = personnelOrgs
            .Where(x => x.SupervisorId.HasValue)
            .GroupBy(x => x.CustomerPersonnelId)
            .ToDictionary(g => g.Key, g => g.First().SupervisorId!.Value);

        // Supervisor'lar da personel listesinde olmalı
        var allSupervisorIds = supervisorMap.Values.Distinct().ToList();
        foreach (var supId in allSupervisorIds)
        {
            if (!personnelIds.Contains(supId))
                personnelIds.Add(supId);
        }

        // Kök düğümler = supervisor'ı olmayan veya supervisor'ı listeye dahil olmayan kişiler
        var participantMap = new Dictionary<int, AssessmentParticipant>();
        var order = 1;

        // Önce tüm katılımcıları oluştur (ParentId'siz)
        foreach (var personnelId in personnelIds)
        {
            var participant = new AssessmentParticipant
            {
                AssignmentPeriodId = assignmentPeriodId,
                CustomerPersonnelId = personnelId,
                Order = order++
            };
            _context.AssessmentParticipants.Add(participant);
            participantMap[personnelId] = participant;
        }

        await _context.SaveChangesAsync();

        // Şimdi ParentId'leri set et (supervisor → parent)
        foreach (var kvp in supervisorMap)
        {
            var personnelId = kvp.Key;
            var supervisorId = kvp.Value;

            if (participantMap.ContainsKey(personnelId) && participantMap.ContainsKey(supervisorId))
            {
                participantMap[personnelId].ParentId = participantMap[supervisorId].Id;
            }
        }

        await _context.SaveChangesAsync();

        return participantMap.Count;
    }

    public async Task<int> GetParticipantCountAsync(int assignmentPeriodId)
    {
        return await _context.AssessmentParticipants
            .CountAsync(x => x.AssignmentPeriodId == assignmentPeriodId);
    }

    // ===== AssessmentTask Zincir Oluşturma =====

    public async Task<int> GenerateAssessmentTasksAsync(int assignmentPeriodId, int projectId)
    {
        var project = await _context.Projects
            .FirstOrDefaultAsync(x => x.Id == projectId);
        if (project == null)
            throw new InvalidOperationException("Proje bulunamadı.");

        var isAnonymous = project.IsAnonymous;
        var is360 = project.AssessmentModeId == AssessmentModes.Ids.Mode360;

        // Dönemdeki tüm katılımcıları al
        var participants = await _context.AssessmentParticipants
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId)
            .ToListAsync();

        if (!participants.Any())
            throw new InvalidOperationException("Dönemde katılımcı bulunmuyor.");

        var totalTaskCount = 0;

        foreach (var participant in participants)
        {
            // Bu katılımcı için mevcut davetiye var mı kontrol et
            var existingInvitation = await _context.SurveyInvitations
                .AnyAsync(x => x.ProjectId == projectId
                            && x.CustomerPersonnelId == participant.CustomerPersonnelId
                            && !x.IsCompleted);
            if (existingInvitation) continue;

            // Davetiye oluştur
            var invitation = new SurveyInvitation
            {
                ProjectId = projectId,
                CustomerPersonnelId = participant.CustomerPersonnelId,
                StatusId = SurveyInvitationStatuses.Ids.Pending
            };
            _context.SurveyInvitations.Add(invitation);
            await _context.SaveChangesAsync();

            var order = 1;

            // 1. Self — her zaman ilk
            _context.AssessmentTasks.Add(new AssessmentTask
            {
                SurveyInvitationId = invitation.Id,
                EvaluatedCustomerPersonnelId = participant.CustomerPersonnelId,
                FeedbackRoleId = FeedbackRoles.Ids.Self,
                Order = order++
            });

            // 2. Manager — üstünü değerlendir (ParentId varsa)
            if (participant.ParentId.HasValue)
            {
                var parent = participants.FirstOrDefault(x => x.Id == participant.ParentId.Value);
                if (parent != null)
                {
                    _context.AssessmentTasks.Add(new AssessmentTask
                    {
                        SurveyInvitationId = invitation.Id,
                        EvaluatedCustomerPersonnelId = parent.CustomerPersonnelId,
                        FeedbackRoleId = FeedbackRoles.Ids.Manager,
                        Order = order++
                    });
                }
            }

            // 360° modda ek task'lar
            if (is360)
            {
                // 3. Peer — aynı ParentId'ye sahip kardeşler
                var peers = participants
                    .Where(x => x.ParentId == participant.ParentId
                             && x.Id != participant.Id)
                    .ToList();

                if (peers.Any())
                {
                    if (isAnonymous)
                    {
                        // Anonim: toplu tek form (EvaluatedPerson = null)
                        _context.AssessmentTasks.Add(new AssessmentTask
                        {
                            SurveyInvitationId = invitation.Id,
                            EvaluatedCustomerPersonnelId = null,
                            FeedbackRoleId = FeedbackRoles.Ids.Peer,
                            Order = order++
                        });
                    }
                    else
                    {
                        // İsimli: her peer için ayrı form
                        foreach (var peer in peers)
                        {
                            _context.AssessmentTasks.Add(new AssessmentTask
                            {
                                SurveyInvitationId = invitation.Id,
                                EvaluatedCustomerPersonnelId = peer.CustomerPersonnelId,
                                FeedbackRoleId = FeedbackRoles.Ids.Peer,
                                Order = order++
                            });
                        }
                    }
                }

                // 4. Subordinate — ParentId'si kendisine bakan çocuklar
                var subordinates = participants
                    .Where(x => x.ParentId == participant.Id)
                    .ToList();

                if (subordinates.Any())
                {
                    if (isAnonymous)
                    {
                        // Anonim: toplu tek form (EvaluatedPerson = null)
                        _context.AssessmentTasks.Add(new AssessmentTask
                        {
                            SurveyInvitationId = invitation.Id,
                            EvaluatedCustomerPersonnelId = null,
                            FeedbackRoleId = FeedbackRoles.Ids.Subordinate,
                            Order = order++
                        });
                    }
                    else
                    {
                        // İsimli: her subordinate için ayrı form
                        foreach (var sub in subordinates)
                        {
                            _context.AssessmentTasks.Add(new AssessmentTask
                            {
                                SurveyInvitationId = invitation.Id,
                                EvaluatedCustomerPersonnelId = sub.CustomerPersonnelId,
                                FeedbackRoleId = FeedbackRoles.Ids.Subordinate,
                                Order = order++
                            });
                        }
                    }
                }
            }

            totalTaskCount += order - 1;
        }

        await _context.SaveChangesAsync();
        return totalTaskCount;
    }

    public async Task<List<AssessmentTask>> GetTaskChainAsync(int surveyInvitationId)
    {
        return await _context.AssessmentTasks
            .Include(x => x.EvaluatedCustomerPersonnel)
            .Where(x => x.SurveyInvitationId == surveyInvitationId)
            .OrderBy(x => x.Order)
            .ToListAsync();
    }

    public async Task<AssessmentTask?> GetNextPendingTaskAsync(int surveyInvitationId)
    {
        return await _context.AssessmentTasks
            .Include(x => x.EvaluatedCustomerPersonnel)
            .Where(x => x.SurveyInvitationId == surveyInvitationId && !x.IsCompleted)
            .OrderBy(x => x.Order)
            .FirstOrDefaultAsync();
    }

    // ===== Private Helpers =====

    private async Task RemoveChildrenRecursiveAsync(int parentId)
    {
        var children = await _context.AssessmentParticipants
            .Where(x => x.ParentId == parentId)
            .ToListAsync();

        foreach (var child in children)
        {
            await RemoveChildrenRecursiveAsync(child.Id);
            child.IsDeleted = true;
        }
    }

    private async Task<bool> IsDescendantAsync(int candidateId, int ancestorId)
    {
        // candidateId, ancestorId'nin alt ağacında mı?
        var children = await _context.AssessmentParticipants
            .Where(x => x.ParentId == ancestorId)
            .Select(x => x.Id)
            .ToListAsync();

        if (children.Contains(candidateId)) return true;

        foreach (var childId in children)
        {
            if (await IsDescendantAsync(candidateId, childId))
                return true;
        }

        return false;
    }
}

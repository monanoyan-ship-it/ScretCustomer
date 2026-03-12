using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.AssignmentPeriod;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;
using SecretCustomer.Services.Helpers;

namespace SecretCustomer.Services.Services;

public class AssessmentService : IAssessmentService
{
    private readonly ApplicationDbContext _context;

    public AssessmentService(ApplicationDbContext context)
    {
        _context = context;
    }

    // ===== Proje Bazlı Dönem Yönetimi (PersonnelAssessment) =====

    public async Task<List<AssignmentPeriodDto>> GetPeriodsByProjectAsync(int projectId)
    {
        return await _context.AssignmentPeriods
            .Where(p => p.ProjectId == projectId && !p.IsDeleted)
            .OrderByDescending(p => p.Id)
            .Select(p => new AssignmentPeriodDto
            {
                Id = p.Id,
                AssignmentId = p.AssignmentId,
                ProjectId = p.ProjectId,
                Name = p.Name,
                StartDate = p.StartDate,
                EndDate = p.EndDate,
                Status = PeriodStatuses.GetById(p.StatusId) != null ? PeriodStatuses.GetById(p.StatusId)!.SystemName : "Open",
                StatusName = p.StatusId == PeriodStatuses.Ids.Open ? "Açık" : "Kapalı",
                TargetCount = p.TargetCount,
                CompletedCount = p.CompletedCount,
                AverageScore = p.AverageScore,
                Notes = p.Notes,
                CreatedByUserId = p.CreatedByUserId,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<AssignmentPeriodDto> CreatePeriodForProjectAsync(int projectId, CreateAssignmentPeriodDto dto)
    {
        var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && !p.IsDeleted);
        if (project == null)
            throw new InvalidOperationException("Proje bulunamadı.");

        if (project.ProjectTypeId != ProjectTypes.Ids.PersonnelAssessment)
            throw new InvalidOperationException("Bu işlem sadece Personel Değerlendirme projelerinde kullanılabilir.");

        var period = new AssignmentPeriod
        {
            ProjectId = projectId,
            AssignmentId = null,
            Name = dto.Name,
            StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc),
            EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc),
            StatusId = PeriodStatuses.Ids.Open,
            TargetCount = dto.TargetCount,
            Notes = dto.Notes,
            CreatedAt = TurkeyTime.Now
        };

        _context.AssignmentPeriods.Add(period);
        await _context.SaveChangesAsync();

        return new AssignmentPeriodDto
        {
            Id = period.Id,
            ProjectId = period.ProjectId,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = "Open",
            StatusName = "Açık",
            TargetCount = period.TargetCount,
            CompletedCount = 0,
            Notes = period.Notes,
            CreatedAt = period.CreatedAt
        };
    }

    public async Task<AssignmentPeriodDto?> UpdatePeriodAsync(int periodId, UpdateAssignmentPeriodDto dto)
    {
        var period = await _context.AssignmentPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted);

        if (period == null) return null;

        period.Name = dto.Name;
        period.StartDate = DateTime.SpecifyKind(dto.StartDate, DateTimeKind.Utc);
        period.EndDate = DateTime.SpecifyKind(dto.EndDate, DateTimeKind.Utc);
        period.TargetCount = dto.TargetCount;
        period.Notes = dto.Notes;
        period.StatusId = dto.Status == "Closed" ? PeriodStatuses.Ids.Closed : PeriodStatuses.Ids.Open;
        period.UpdatedAt = TurkeyTime.Now;

        await _context.SaveChangesAsync();

        return new AssignmentPeriodDto
        {
            Id = period.Id,
            AssignmentId = period.AssignmentId,
            ProjectId = period.ProjectId,
            Name = period.Name,
            StartDate = period.StartDate,
            EndDate = period.EndDate,
            Status = PeriodStatuses.GetById(period.StatusId)?.SystemName ?? "Open",
            StatusName = period.StatusId == PeriodStatuses.Ids.Open ? "Açık" : "Kapalı",
            TargetCount = period.TargetCount,
            CompletedCount = period.CompletedCount,
            AverageScore = period.AverageScore,
            Notes = period.Notes,
            CreatedAt = period.CreatedAt
        };
    }

    public async Task<bool> DeletePeriodAsync(int periodId)
    {
        var period = await _context.AssignmentPeriods
            .FirstOrDefaultAsync(p => p.Id == periodId && !p.IsDeleted);

        if (period == null) return false;

        period.IsDeleted = true;
        period.UpdatedAt = TurkeyTime.Now;
        await _context.SaveChangesAsync();
        return true;
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
        // Aynı dönemde aynı kişi olmamalı (soft-deleted dahil kontrol - unique constraint IsDeleted filtresiz)
        var existing = await _context.AssessmentParticipants
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(x => x.AssignmentPeriodId == assignmentPeriodId
                        && x.CustomerPersonnelId == customerPersonnelId);
        if (existing != null)
        {
            if (!existing.IsDeleted)
                throw new InvalidOperationException("Bu kişi zaten bu dönemde katılımcı olarak eklenmiş.");
            // Soft-deleted kaydı geri getir
            existing.IsDeleted = false;
            existing.ParentId = parentId;
            existing.UpdatedAt = Core.Helpers.TurkeyTime.Now;
            await _context.SaveChangesAsync();
            return existing;
        }

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

        // Alt katılımcıları da soft-delete
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

        // Mevcut dönem katılımcılarını soft-delete yap
        var existingParticipants = await _context.AssessmentParticipants
            .IgnoreQueryFilters()
            .Where(x => x.AssignmentPeriodId == assignmentPeriodId)
            .ToListAsync();
        // Soft-deleted kayıt index'i (geri getirmek için)
        var softDeletedMap = existingParticipants
            .ToDictionary(x => x.CustomerPersonnelId, x => x);
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

        // Soft-deleted varsa geri getir, yoksa yeni oluştur
        foreach (var personnelId in personnelIds)
        {
            if (softDeletedMap.TryGetValue(personnelId, out var existing))
            {
                // Geri getir
                existing.IsDeleted = false;
                existing.ParentId = null; // Sonra set edilecek
                existing.Order = order++;
                existing.UpdatedAt = Core.Helpers.TurkeyTime.Now;
                participantMap[personnelId] = existing;
            }
            else
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
            var existingInv = await _context.SurveyInvitations
                .FirstOrDefaultAsync(x => x.ProjectId == projectId
                            && x.CustomerPersonnelId == participant.CustomerPersonnelId
                            && !x.IsCompleted);

            SurveyInvitation invitation;
            if (existingInv != null)
            {
                // Mevcut davetiyede task var mı?
                var hasExistingTasks = await _context.AssessmentTasks.AnyAsync(t => t.SurveyInvitationId == existingInv.Id);
                if (hasExistingTasks) continue; // Zaten task'ları olan davetiye, atla

                // Task'sız davetiye var (SendInvitations ile oluşturulmuş olabilir), task'ları ekle
                invitation = existingInv;
            }
            else
            {
                // Yeni davetiye oluştur
                invitation = new SurveyInvitation
                {
                    ProjectId = projectId,
                    CustomerPersonnelId = participant.CustomerPersonnelId,
                    StatusId = SurveyInvitationStatuses.Ids.Pending
                };
                _context.SurveyInvitations.Add(invitation);
                await _context.SaveChangesAsync();
            }

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

    // ===== Davetiye Yönetimi =====

    public async Task<Dictionary<int, SurveyInvitation>> GetOpenInvitationsByProjectAsync(int projectId)
    {
        var invitations = await _context.SurveyInvitations
            .Where(x => x.ProjectId == projectId && !x.IsCompleted)
            .ToListAsync();

        // Group by personnel and take the oldest invitation per person (from task generation)
        return invitations
            .GroupBy(x => x.CustomerPersonnelId)
            .ToDictionary(g => g.Key, g => g.OrderBy(x => x.Id).First());
    }

    // ===== Tek Link Doldurma Akışı =====

    public async Task<AssessmentFillContext?> GetFillContextByTokenAsync(string token)
    {
        // Token'ı çöz → projectId + personnelId
        var tokenData = EncryptionHelper.ParseSurveyToken(token);
        if (tokenData == null) return null;

        // Proje bitiş tarihine göre expiration kontrolü
        var project = await _context.Projects
            .Include(x => x.Checklist)
                .ThenInclude(c => c!.Questions.Where(q => !q.IsDeleted))
                    .ThenInclude(q => q.SubCriteria.Where(sc => !sc.IsDeleted))
            .Include(x => x.Customer)
            .FirstOrDefaultAsync(x => x.Id == tokenData.ProjectId);

        if (project == null) return null;

        if (tokenData.IsExpiredByDate(project.EndDate))
            return null;

        // SurveyInvitation bul — task'ları olan davetiyeyi tercih et
        var invitations = await _context.SurveyInvitations
            .Include(x => x.CustomerPersonnel)
            .Where(x => x.ProjectId == tokenData.ProjectId
                      && x.CustomerPersonnelId == tokenData.PersonnelId
                      && !x.IsCompleted)
            .ToListAsync();

        if (!invitations.Any()) return null;

        // Task'ları olan davetiyeyi bul (GenerateAssessmentTasks ile oluşturulan)
        SurveyInvitation? invitation = null;
        foreach (var inv in invitations.OrderBy(x => x.Id))
        {
            var hasAnyTask = await _context.AssessmentTasks.AnyAsync(t => t.SurveyInvitationId == inv.Id);
            if (hasAnyTask) { invitation = inv; break; }
        }
        // Hiç task'ı olan davetiye yoksa ilkini kullan
        invitation ??= invitations.OrderBy(x => x.Id).First();

        // Açıldı olarak işaretle
        if (!invitation.IsOpened)
        {
            invitation.IsOpened = true;
            invitation.OpenedAt = TurkeyTime.Now;
            await _context.SaveChangesAsync();
        }

        // Task zincirini getir
        var tasks = await _context.AssessmentTasks
            .Include(x => x.EvaluatedCustomerPersonnel)
            .Where(x => x.SurveyInvitationId == invitation.Id)
            .OrderBy(x => x.Order)
            .ToListAsync();

        var totalCount = tasks.Count;
        var completedCount = tasks.Count(x => x.IsCompleted);
        var currentTask = tasks.FirstOrDefault(x => !x.IsCompleted);

        if (currentTask == null)
        {
            if (totalCount == 0)
            {
                // Davetiye var ama task zinciri oluşturulmamış
                return null;
            }

            // Tüm task'lar tamamlanmış
            return new AssessmentFillContext
            {
                Invitation = invitation,
                Project = project,
                CurrentTask = tasks.Last(), // son tamamlanan
                TotalTaskCount = totalCount,
                CompletedTaskCount = completedCount,
                IsAllCompleted = true
            };
        }

        return new AssessmentFillContext
        {
            Invitation = invitation,
            Project = project,
            CurrentTask = currentTask,
            TotalTaskCount = totalCount,
            CompletedTaskCount = completedCount,
            IsAllCompleted = false
        };
    }

    public async Task<AssessmentTaskCompletionResult> CompleteTaskAsync(int assessmentTaskId, int evaluationId)
    {
        var task = await _context.AssessmentTasks
            .FirstOrDefaultAsync(x => x.Id == assessmentTaskId);

        if (task == null)
            throw new InvalidOperationException("Değerlendirme görevi bulunamadı.");

        if (task.IsCompleted)
            throw new InvalidOperationException("Bu görev zaten tamamlanmış.");

        // Task'ı tamamla
        task.EvaluationId = evaluationId;
        task.IsCompleted = true;
        task.CompletedAt = TurkeyTime.Now;

        // Sıradaki task'ı kontrol et
        var nextTask = await _context.AssessmentTasks
            .Include(x => x.EvaluatedCustomerPersonnel)
            .Where(x => x.SurveyInvitationId == task.SurveyInvitationId
                     && !x.IsCompleted
                     && x.Id != assessmentTaskId)
            .OrderBy(x => x.Order)
            .FirstOrDefaultAsync();

        var isAllCompleted = nextTask == null;

        // Tümü tamamlandıysa davetiyeyi de kapat
        if (isAllCompleted)
        {
            var invitation = await _context.SurveyInvitations
                .FirstOrDefaultAsync(x => x.Id == task.SurveyInvitationId);

            if (invitation != null)
            {
                invitation.IsCompleted = true;
                invitation.CompletedAt = TurkeyTime.Now;
            }
        }

        await _context.SaveChangesAsync();

        return new AssessmentTaskCompletionResult
        {
            HasNextTask = !isAllCompleted,
            NextTask = nextTask,
            IsAllCompleted = isAllCompleted
        };
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

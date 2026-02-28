using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using SecretCustomer.Core.DTOs.EvaluationImport;
using SecretCustomer.Core.Entities;
using SecretCustomer.Core.Enums;
using SecretCustomer.Core.Helpers;
using SecretCustomer.Core.Interfaces.Services;
using SecretCustomer.Data;

namespace SecretCustomer.Services.Services;

public class EvaluationImportService : IEvaluationImportService
{
    private readonly ApplicationDbContext _context;

    public EvaluationImportService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<EvaluationImportSessionDto> UploadAndProcessAsync(byte[] fileContent, string fileName, int uploadedByUserId, int? customerId = null)
    {
        // First detect customer BEFORE creating session
        using var stream = new MemoryStream(fileContent);
        using var workbook = new XLWorkbook(stream);
        var worksheet = workbook.Worksheets.First();

        var lastRow = worksheet.LastRowUsed()?.RowNumber() ?? 1;
        var dataStartRow = 2; // skip header

        int? detectedCustomerId = customerId;

        if (!detectedCustomerId.HasValue)
        {
            var customers = await _context.Customers
                .Where(c => !c.IsDeleted)
                .Select(c => new { c.Id, c.CompanyName })
                .ToListAsync();

            var firstFirmaCell = GetCellValue(worksheet, dataStartRow, 1)?.Trim();
            if (!string.IsNullOrWhiteSpace(firstFirmaCell))
            {
                var matchedCustomer = customers.FirstOrDefault(c =>
                    c.CompanyName.Equals(firstFirmaCell, StringComparison.OrdinalIgnoreCase));
                detectedCustomerId = matchedCustomer?.Id;
            }
        }

        if (!detectedCustomerId.HasValue)
        {
            throw new InvalidOperationException("CUSTOMER_REQUIRED");
        }

        // Now create session
        var session = new EvaluationImportSession
        {
            FileName = fileName,
            StatusId = EvaluationImportSessionStatuses.Ids.Processing,
            CustomerId = detectedCustomerId.Value,
            UploadedByUserId = uploadedByUserId,
            CreatedAt = TurkeyTime.Now
        };
        _context.EvaluationImportSessions.Add(session);
        await _context.SaveChangesAsync();

        try
        {
            // Pre-load lookup data
            var projects = await _context.Projects
                .Where(p => !p.IsDeleted)
                .Select(p => new { p.Id, p.Name, p.ChecklistId })
                .ToListAsync();

            var users = await _context.Users
                .Where(u => !u.IsDeleted)
                .Select(u => new { u.Id, u.FirstName, u.LastName })
                .ToListAsync();

            var customerPersonnel = await _context.CustomerPersonnel
                .Where(cp => !cp.IsDeleted)
                .Select(cp => new { cp.Id, cp.FirstName, cp.LastName, cp.CustomerId })
                .ToListAsync();

            // Pre-load existing CallIds for duplicate detection
            var existingCallIds = (await _context.Evaluations
                .Where(e => !e.IsDeleted && e.CallId != null)
                .Select(e => e.CallId!)
                .ToListAsync()).ToHashSet();

            // Filter personnel by customer for more accurate matching
            var personnelForMatching = detectedCustomerId.HasValue
                ? customerPersonnel.Where(cp => cp.CustomerId == detectedCustomerId.Value).ToList()
                : customerPersonnel;

            var pendingRows = new List<EvaluationImportPendingRow>();
            var unmatchedDict = new Dictionary<string, EvaluationImportUnmatchedItem>();
            var evaluationsToCreate = new List<Evaluation>();
            var directImportRows = new List<EvaluationImportPendingRow>();

            int totalRows = 0;
            int importedRows = 0;
            int pendingCount = 0;
            int skippedDuplicates = 0;

            for (int row = dataStartRow; row <= lastRow; row++)
            {
                var cellA = GetCellValue(worksheet, row, 1); // Firma
                var cellD = GetCellValue(worksheet, row, 4); // Değerlendirme Yapan
                var cellE = GetCellValue(worksheet, row, 5); // Kişi
                var cellF = GetCellValue(worksheet, row, 6); // Çağrı No
                var cellG = GetCellValue(worksheet, row, 7); // Değerlendirilen (proje parse)
                var cellH = GetCellDateValue(worksheet, row, 8); // Kontrol Tarihi
                var cellI = GetCellTimeValue(worksheet, row, 9); // Saat
                var cellJ = GetCellTimeValue(worksheet, row, 10); // Süre
                var cellK = GetCellValue(worksheet, row, 11); // Yorum
                var cellL = GetCellValue(worksheet, row, 12); // Periyot
                var cellM = GetCellValue(worksheet, row, 13); // Periyot (Ay)
                var cellN = GetCellDateValue(worksheet, row, 14); // Oluşturma Tarihi
                var cellO = GetCellTimeValue(worksheet, row, 15); // Oluşturma Saati
                var cellP = GetCellDateValue(worksheet, row, 16); // Değişiklik Tarihi
                var cellQ = GetCellValue(worksheet, row, 17); // Ortalama Puan

                // Skip empty rows
                if (string.IsNullOrWhiteSpace(cellA) && string.IsNullOrWhiteSpace(cellG))
                    continue;

                totalRows++;

                // Parse G kolonu → proje adı
                var parsedProjectName = ParseProjectName(cellG);
                var matchedProject = projects
                    .FirstOrDefault(p => NormalizeName(p.Name).Equals(NormalizeName(parsedProjectName), StringComparison.OrdinalIgnoreCase));

                // Parse D kolonu → evaluator (Users tablosunda ara)
                var parsedEvaluatorName = ParseEvaluatorName(cellD);
                var evaluatorParts = parsedEvaluatorName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var matchedUser = evaluatorParts?.Length >= 2
                    ? users.FirstOrDefault(u =>
                        u.FirstName.Equals(evaluatorParts[0], StringComparison.OrdinalIgnoreCase) &&
                        u.LastName.Equals(evaluatorParts[1], StringComparison.OrdinalIgnoreCase))
                    : evaluatorParts?.Length == 1
                        ? users.FirstOrDefault(u =>
                            u.FirstName.Equals(evaluatorParts[0], StringComparison.OrdinalIgnoreCase))
                        : null;

                // Parse E kolonu → kişi (customer bazlı filtrelenmiş listede ara)
                var parsedPersonName = cellE?.Trim();
                var personParts = parsedPersonName?.Split(' ', 2, StringSplitOptions.RemoveEmptyEntries);
                var matchedPerson = personParts?.Length >= 2
                    ? personnelForMatching.FirstOrDefault(cp =>
                        cp.FirstName.Equals(personParts[0], StringComparison.OrdinalIgnoreCase) &&
                        cp.LastName.Equals(personParts[1], StringComparison.OrdinalIgnoreCase))
                    : personParts?.Length == 1
                        ? personnelForMatching.FirstOrDefault(cp =>
                            cp.FirstName.Equals(personParts[0], StringComparison.OrdinalIgnoreCase))
                        : null;

                // Parse dates (cellH, cellN, cellP are already DateTime? from GetCellDateValue)
                var parsedCallDate = cellH;
                var parsedCallTime = cellI;
                var parsedCreatedDate = cellN.HasValue && !string.IsNullOrWhiteSpace(cellO)
                    ? (TimeSpan.TryParse(cellO, out var createdTime) ? cellN.Value.Add(createdTime) : cellN)
                    : cellN;
                var parsedModifiedDate = cellP;
                var parsedScore = ParseDecimal(cellQ);

                var rawData = new Dictionary<string, string?>
                {
                    ["A"] = cellA, ["D"] = cellD, ["E"] = cellE, ["F"] = cellF,
                    ["G"] = cellG, ["H"] = cellH?.ToString("dd.MM.yyyy"), ["I"] = cellI, ["J"] = cellJ,
                    ["K"] = cellK, ["L"] = cellL, ["M"] = cellM, ["N"] = cellN?.ToString("dd.MM.yyyy"),
                    ["O"] = cellO, ["P"] = cellP?.ToString("dd.MM.yyyy"), ["Q"] = cellQ
                };

                var pendingRow = new EvaluationImportPendingRow
                {
                    ImportSessionId = session.Id,
                    RowNumber = row,
                    RawDataJson = JsonSerializer.Serialize(rawData),
                    ParsedProjectName = parsedProjectName,
                    ParsedEvaluatorName = parsedEvaluatorName,
                    ParsedEvaluatedPersonName = parsedPersonName,
                    ParsedCallId = cellF?.Trim(),
                    ParsedCallDate = parsedCallDate.HasValue ? DateTime.SpecifyKind(parsedCallDate.Value, DateTimeKind.Utc) : null,
                    ParsedCallTime = parsedCallTime,
                    ParsedDuration = cellJ?.Trim(),
                    ParsedComment = cellK?.Trim(),
                    ParsedScore = parsedScore,
                    ParsedPeriod = cellL?.Trim(),
                    ParsedPeriodMonth = cellM?.Trim(),
                    ParsedCreatedDate = parsedCreatedDate.HasValue ? DateTime.SpecifyKind(parsedCreatedDate.Value, DateTimeKind.Utc) : null,
                    ParsedModifiedDate = parsedModifiedDate.HasValue ? DateTime.SpecifyKind(parsedModifiedDate.Value, DateTimeKind.Utc) : null,
                    MatchedProjectId = matchedProject?.Id,
                    MatchedEvaluatorId = matchedUser?.Id,
                    MatchedCustomerPersonnelId = matchedPerson?.Id,
                    UnmatchedProjectValue = matchedProject == null ? parsedProjectName : null,
                    UnmatchedEvaluatorValue = matchedUser == null ? parsedEvaluatorName : null,
                    UnmatchedPersonValue = matchedPerson == null ? parsedPersonName : null,
                    StatusId = EvaluationImportRowStatuses.Ids.Pending,
                    CreatedAt = TurkeyTime.Now
                };

                // Duplicate check: skip if CallId already exists
                var callId = cellF?.Trim();
                if (!string.IsNullOrWhiteSpace(callId) && existingCallIds.Contains(callId))
                {
                    pendingRow.StatusId = EvaluationImportRowStatuses.Ids.Skipped;
                    pendingRows.Add(pendingRow);
                    skippedDuplicates++;
                    continue;
                }

                bool allMatched = matchedProject != null && matchedUser != null && matchedPerson != null;

                if (allMatched)
                {
                    // Direct import - create evaluation
                    var evaluation = CreateEvaluation(
                        matchedProject!.Id, matchedProject.ChecklistId,
                        matchedUser!.Id, matchedPerson!.Id,
                        pendingRow);

                    evaluationsToCreate.Add(evaluation);
                    pendingRow.StatusId = EvaluationImportRowStatuses.Ids.Imported;
                    directImportRows.Add(pendingRow);
                    importedRows++;

                    // Track newly created CallId to prevent intra-file duplicates
                    if (!string.IsNullOrWhiteSpace(callId))
                        existingCallIds.Add(callId);
                }
                else
                {
                    // Track unmatched items
                    if (matchedProject == null && !string.IsNullOrWhiteSpace(parsedProjectName))
                    {
                        TrackUnmatchedItem(unmatchedDict, session.Id,
                            EvaluationImportUnmatchedItemTypes.Ids.Project, parsedProjectName);
                    }
                    if (matchedUser == null && !string.IsNullOrWhiteSpace(parsedEvaluatorName))
                    {
                        TrackUnmatchedItem(unmatchedDict, session.Id,
                            EvaluationImportUnmatchedItemTypes.Ids.Evaluator, parsedEvaluatorName);
                    }
                    if (matchedPerson == null && !string.IsNullOrWhiteSpace(parsedPersonName))
                    {
                        TrackUnmatchedItem(unmatchedDict, session.Id,
                            EvaluationImportUnmatchedItemTypes.Ids.EvaluatedPerson, parsedPersonName);
                    }
                    pendingCount++;
                }

                pendingRows.Add(pendingRow);
            }

            // Save evaluations
            if (evaluationsToCreate.Count > 0)
            {
                _context.Evaluations.AddRange(evaluationsToCreate);
                await _context.SaveChangesAsync();

                // Link evaluation IDs back to pending rows
                for (int i = 0; i < directImportRows.Count; i++)
                {
                    directImportRows[i].EvaluationId = evaluationsToCreate[i].Id;
                }
            }

            // Save pending rows
            _context.EvaluationImportPendingRows.AddRange(pendingRows);

            // Save unmatched items
            var unmatchedItems = unmatchedDict.Values.ToList();
            if (unmatchedItems.Count > 0)
            {
                _context.EvaluationImportUnmatchedItems.AddRange(unmatchedItems);
            }

            // Update session
            session.TotalRows = totalRows;
            session.ImportedRows = importedRows;
            session.PendingRows = pendingCount;
            session.SkippedRows = skippedDuplicates;
            session.StatusId = pendingCount > 0
                ? EvaluationImportSessionStatuses.Ids.Pending
                : EvaluationImportSessionStatuses.Ids.Completed;
            if (skippedDuplicates > 0)
                session.Notes = $"{skippedDuplicates} satır zaten mevcut (CallId tekrarı) olduğu için atlandı.";

            await _context.SaveChangesAsync();

            return MapToSessionDto(session);
        }
        catch (Exception ex)
        {
            session.StatusId = EvaluationImportSessionStatuses.Ids.Failed;
            session.Notes = ex.Message;
            await _context.SaveChangesAsync();
            throw;
        }
    }

    public async Task<List<EvaluationImportSessionDto>> GetSessionsAsync()
    {
        var sessions = await _context.EvaluationImportSessions
            .Where(s => !s.IsDeleted)
            .Include(s => s.Customer)
            .Include(s => s.UploadedByUser)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync();

        return sessions.Select(MapToSessionDto).ToList();
    }

    public async Task<EvaluationImportSessionDetailDto> GetSessionDetailAsync(int sessionId)
    {
        var session = await _context.EvaluationImportSessions
            .Where(s => !s.IsDeleted && s.Id == sessionId)
            .Include(s => s.Customer)
            .Include(s => s.UploadedByUser)
            .FirstOrDefaultAsync()
            ?? throw new KeyNotFoundException($"Import session {sessionId} bulunamadı.");

        var unmatchedItems = await _context.EvaluationImportUnmatchedItems
            .Where(u => !u.IsDeleted && u.ImportSessionId == sessionId)
            .ToListAsync();

        var readyToImportCount = await _context.EvaluationImportPendingRows
            .CountAsync(r => !r.IsDeleted
                && r.ImportSessionId == sessionId
                && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                && r.UnmatchedProjectValue == null
                && r.UnmatchedEvaluatorValue == null
                && r.UnmatchedPersonValue == null);

        var dto = new EvaluationImportSessionDetailDto
        {
            Id = session.Id,
            CustomerId = session.CustomerId,
            CustomerName = session.Customer?.CompanyName,
            FileName = session.FileName,
            StatusId = session.StatusId,
            StatusName = EvaluationImportSessionStatuses.GetById(session.StatusId)?.Description,
            StatusBadgeClass = EvaluationImportSessionStatuses.GetById(session.StatusId)?.CssClass,
            TotalRows = session.TotalRows,
            ImportedRows = session.ImportedRows,
            PendingRows = session.PendingRows,
            SkippedRows = session.SkippedRows,
            Notes = session.Notes,
            UploadedByUserId = session.UploadedByUserId,
            UploadedByUserName = session.UploadedByUser != null
                ? $"{session.UploadedByUser.FirstName} {session.UploadedByUser.LastName}"
                : null,
            CreatedAt = session.CreatedAt,
            UnmatchedPersonCount = unmatchedItems.Count(u => u.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.EvaluatedPerson && !u.ResolutionActionId.HasValue),
            UnmatchedEvaluatorCount = unmatchedItems.Count(u => u.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Evaluator && !u.ResolutionActionId.HasValue),
            UnmatchedProjectCount = unmatchedItems.Count(u => u.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Project && !u.ResolutionActionId.HasValue),
            ResolvedItemCount = unmatchedItems.Count(u => u.ResolutionActionId.HasValue),
            ReadyToImportRowCount = readyToImportCount
        };

        return dto;
    }

    public async Task<PagedUnmatchedItemResult> GetUnmatchedItemsAsync(int sessionId, int? itemTypeId = null, int page = 1, int pageSize = 50)
    {
        var query = _context.EvaluationImportUnmatchedItems
            .Where(u => !u.IsDeleted && u.ImportSessionId == sessionId);

        if (itemTypeId.HasValue)
            query = query.Where(u => u.ItemTypeId == itemTypeId.Value);

        var totalCount = await query.CountAsync();

        var items = await query
            .Include(u => u.ResolvedByUser)
            .OrderBy(u => u.ResolutionActionId.HasValue)
            .ThenBy(u => u.OriginalValue)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedUnmatchedItemResult
        {
            Items = items.Select(u => new EvaluationImportUnmatchedItemDto
            {
                Id = u.Id,
                ImportSessionId = u.ImportSessionId,
                ItemTypeId = u.ItemTypeId,
                ItemTypeName = EvaluationImportUnmatchedItemTypes.GetById(u.ItemTypeId)?.Description,
                OriginalValue = u.OriginalValue,
                AffectedRowCount = u.AffectedRowCount,
                ResolvedEntityId = u.ResolvedEntityId,
                ResolutionActionId = u.ResolutionActionId,
                ResolutionActionName = u.ResolutionActionId.HasValue
                    ? EvaluationImportResolutionActions.GetById(u.ResolutionActionId.Value)?.Description
                    : null,
                ResolvedAt = u.ResolvedAt,
                ResolvedByUserId = u.ResolvedByUserId,
                ResolvedByUserName = u.ResolvedByUser != null
                    ? $"{u.ResolvedByUser.FirstName} {u.ResolvedByUser.LastName}"
                    : null,
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<EvaluationImportUnmatchedItemDto> ResolveUnmatchedItemAsync(int itemId, ResolveUnmatchedItemDto dto, int userId)
    {
        var item = await _context.EvaluationImportUnmatchedItems
            .Include(u => u.ImportSession)
            .FirstOrDefaultAsync(u => !u.IsDeleted && u.Id == itemId)
            ?? throw new KeyNotFoundException($"Unmatched item {itemId} bulunamadı.");

        int resolvedEntityId;

        if (dto.ActionId == EvaluationImportResolutionActions.Ids.Skipped)
        {
            // Skip - mark all pending rows with this unmatched value as skipped
            resolvedEntityId = 0;
            await SkipPendingRowsForItem(item);
        }
        else if (dto.ActionId == EvaluationImportResolutionActions.Ids.CreatedNew)
        {
            // Create new entity
            if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.EvaluatedPerson)
            {
                var customerId = item.ImportSession.CustomerId
                    ?? throw new InvalidOperationException("Oturumun müşteri bilgisi bulunamadı. Önce bir kişiyi mevcut kayıtla eşleştirin.");

                var firstName = dto.NewFirstName ?? item.OriginalValue.Split(' ', 2)[0];
                var lastName = dto.NewLastName ?? (item.OriginalValue.Split(' ', 2).Length > 1 ? item.OriginalValue.Split(' ', 2)[1] : "");

                // Generate unique username from name (e.g. "ali.veli", "ali.veli.2")
                var baseUsername = GenerateUsername(firstName, lastName);
                var username = baseUsername;
                var suffix = 1;
                while (await _context.CustomerPersonnel.AnyAsync(cp =>
                    !cp.IsDeleted && cp.CustomerId == customerId && cp.Username == username))
                {
                    suffix++;
                    username = $"{baseUsername}.{suffix}";
                }

                // Generate unique email
                var baseEmail = $"{username}@import.local";
                var email = baseEmail;
                var emailSuffix = 1;
                while (await _context.CustomerPersonnel.AnyAsync(cp =>
                    !cp.IsDeleted && cp.CustomerId == customerId && cp.Email == email))
                {
                    emailSuffix++;
                    email = $"{baseUsername}.{emailSuffix}@import.local";
                }

                var newPerson = new CustomerPersonnel
                {
                    FirstName = firstName,
                    LastName = lastName,
                    CustomerId = customerId,
                    Username = username,
                    Email = email,
                    PasswordHash = "-",
                    IsActive = true,
                    CreatedAt = TurkeyTime.Now
                };
                _context.CustomerPersonnel.Add(newPerson);
                await _context.SaveChangesAsync();
                resolvedEntityId = newPerson.Id;
            }
            else if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Project)
            {
                var checklistId = dto.NewProjectChecklistId
                    ?? throw new ArgumentException("Proje oluşturmak için bir kontrol listesi seçilmelidir.");

                var projectName = dto.NewProjectName ?? item.OriginalValue;

                var newProject = new Project
                {
                    Name = projectName,
                    ChecklistId = checklistId,
                    CustomerId = item.ImportSession.CustomerId,
                    ProjectTypeId = ProjectTypes.Ids.CallAuditing,
                    AssignmentTypeId = AssignmentTypes.Ids.CustomerPersonnel,
                    StatusId = ProjectStatuses.Ids.Active,
                    IsActive = true,
                    StartDate = DateTime.SpecifyKind(new DateTime(2020, 1, 1), DateTimeKind.Utc),
                    EndDate = DateTime.SpecifyKind(new DateTime(2030, 12, 31), DateTimeKind.Utc),
                    CreatedAt = TurkeyTime.Now
                };
                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();
                resolvedEntityId = newProject.Id;
            }
            else if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Evaluator)
            {
                var firstName = dto.NewFirstName ?? item.OriginalValue.Split(' ', 2)[0];
                var lastName = dto.NewLastName ?? (item.OriginalValue.Split(' ', 2).Length > 1 ? item.OriginalValue.Split(' ', 2)[1] : "");

                // Generate unique username
                var baseUsername = GenerateUsername(firstName, lastName);
                var username = baseUsername;
                var suffix = 1;
                while (await _context.Users.AnyAsync(u => !u.IsDeleted && u.Username == username))
                {
                    suffix++;
                    username = $"{baseUsername}.{suffix}";
                }

                // Generate unique email
                var baseEmail = $"{baseUsername}@import.local";
                var email = baseEmail;
                var emailSuffix = 1;
                while (await _context.Users.AnyAsync(u => !u.IsDeleted && u.Email == email))
                {
                    emailSuffix++;
                    email = $"{baseUsername}.{emailSuffix}@import.local";
                }

                var newUser = new User
                {
                    FirstName = firstName,
                    LastName = lastName,
                    Username = username,
                    Email = email,
                    PasswordHash = "-",
                    RoleId = UserRoles.Ids.QualitySpecialist,
                    IsActive = true,
                    CreatedAt = TurkeyTime.Now
                };
                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();
                resolvedEntityId = newUser.Id;
            }
            else
            {
                throw new InvalidOperationException("Desteklenmeyen tür için yeni kayıt oluşturulamaz.");
            }
        }
        else
        {
            // LinkedExisting
            resolvedEntityId = dto.EntityId
                ?? throw new ArgumentException("Mevcut kayıtla eşleştirme için EntityId gereklidir.");
        }

        // Update unmatched item
        item.ResolvedEntityId = resolvedEntityId;
        item.ResolutionActionId = dto.ActionId;
        item.ResolvedAt = DateTime.SpecifyKind(TurkeyTime.Now, DateTimeKind.Utc);
        item.ResolvedByUserId = userId;

        if (dto.ActionId != EvaluationImportResolutionActions.Ids.Skipped)
        {
            // Update pending rows
            await UpdatePendingRowsForResolvedItem(item, resolvedEntityId);
        }

        await _context.SaveChangesAsync();

        // Recalculate session stats
        await RecalculateSessionStats(item.ImportSessionId);

        return new EvaluationImportUnmatchedItemDto
        {
            Id = item.Id,
            ImportSessionId = item.ImportSessionId,
            ItemTypeId = item.ItemTypeId,
            ItemTypeName = EvaluationImportUnmatchedItemTypes.GetById(item.ItemTypeId)?.Description,
            OriginalValue = item.OriginalValue,
            AffectedRowCount = item.AffectedRowCount,
            ResolvedEntityId = item.ResolvedEntityId,
            ResolutionActionId = item.ResolutionActionId,
            ResolutionActionName = EvaluationImportResolutionActions.GetById(dto.ActionId)?.Description,
            ResolvedAt = item.ResolvedAt,
            ResolvedByUserId = item.ResolvedByUserId,
        };
    }

    public async Task<PagedPendingRowResult> GetPendingRowsAsync(int sessionId, int? statusId = null, int page = 1, int pageSize = 50)
    {
        var query = _context.EvaluationImportPendingRows
            .Where(r => !r.IsDeleted && r.ImportSessionId == sessionId);

        if (statusId.HasValue)
            query = query.Where(r => r.StatusId == statusId.Value);

        var totalCount = await query.CountAsync();

        var rows = await query
            .Include(r => r.MatchedProject)
            .Include(r => r.MatchedEvaluator)
            .Include(r => r.MatchedCustomerPersonnel)
            .OrderBy(r => r.RowNumber)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedPendingRowResult
        {
            Items = rows.Select(r => new EvaluationImportPendingRowDto
            {
                Id = r.Id,
                ImportSessionId = r.ImportSessionId,
                RowNumber = r.RowNumber,
                ParsedProjectName = r.ParsedProjectName,
                ParsedEvaluatorName = r.ParsedEvaluatorName,
                ParsedEvaluatedPersonName = r.ParsedEvaluatedPersonName,
                ParsedCallId = r.ParsedCallId,
                ParsedCallDate = r.ParsedCallDate,
                ParsedCallTime = r.ParsedCallTime,
                ParsedDuration = r.ParsedDuration,
                ParsedComment = r.ParsedComment,
                ParsedScore = r.ParsedScore,
                ParsedPeriod = r.ParsedPeriod,
                ParsedPeriodMonth = r.ParsedPeriodMonth,
                ParsedCreatedDate = r.ParsedCreatedDate,
                ParsedModifiedDate = r.ParsedModifiedDate,
                MatchedProjectId = r.MatchedProjectId,
                MatchedProjectName = r.MatchedProject?.Name,
                MatchedEvaluatorId = r.MatchedEvaluatorId,
                MatchedEvaluatorName = r.MatchedEvaluator != null
                    ? $"{r.MatchedEvaluator.FirstName} {r.MatchedEvaluator.LastName}" : null,
                MatchedCustomerPersonnelId = r.MatchedCustomerPersonnelId,
                MatchedCustomerPersonnelName = r.MatchedCustomerPersonnel != null
                    ? $"{r.MatchedCustomerPersonnel.FirstName} {r.MatchedCustomerPersonnel.LastName}" : null,
                UnmatchedProjectValue = r.UnmatchedProjectValue,
                UnmatchedEvaluatorValue = r.UnmatchedEvaluatorValue,
                UnmatchedPersonValue = r.UnmatchedPersonValue,
                StatusId = r.StatusId,
                StatusName = EvaluationImportRowStatuses.GetById(r.StatusId)?.Description,
                EvaluationId = r.EvaluationId,
            }).ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<ImportResultDto> ImportResolvedRowsAsync(int sessionId, int userId)
    {
        var result = new ImportResultDto();

        var rows = await _context.EvaluationImportPendingRows
            .Where(r => !r.IsDeleted
                && r.ImportSessionId == sessionId
                && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                && r.UnmatchedProjectValue == null
                && r.UnmatchedEvaluatorValue == null
                && r.UnmatchedPersonValue == null
                && r.MatchedProjectId != null
                && r.MatchedEvaluatorId != null
                && r.MatchedCustomerPersonnelId != null)
            .ToListAsync();

        // Load project checklist mapping
        var projectIds = rows.Select(r => r.MatchedProjectId!.Value).Distinct().ToList();
        var projectChecklistMap = await _context.Projects
            .Where(p => projectIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, p => p.ChecklistId);

        // Pre-load existing CallIds for duplicate detection
        var existingCallIds = (await _context.Evaluations
            .Where(e => !e.IsDeleted && e.CallId != null)
            .Select(e => e.CallId!)
            .ToListAsync()).ToHashSet();

        foreach (var row in rows)
        {
            try
            {
                // Duplicate check: skip if CallId already exists
                if (!string.IsNullOrWhiteSpace(row.ParsedCallId) && existingCallIds.Contains(row.ParsedCallId))
                {
                    row.StatusId = EvaluationImportRowStatuses.Ids.Skipped;
                    result.FailedCount++;
                    result.Errors.Add($"Satır {row.RowNumber}: CallId '{row.ParsedCallId}' zaten mevcut, tekrar atlandı.");
                    continue;
                }

                if (!projectChecklistMap.TryGetValue(row.MatchedProjectId!.Value, out var checklistId))
                {
                    result.FailedCount++;
                    result.Errors.Add($"Satır {row.RowNumber}: Proje #{row.MatchedProjectId} için checklist bulunamadı.");
                    continue;
                }

                var evaluation = CreateEvaluation(
                    row.MatchedProjectId!.Value, checklistId,
                    row.MatchedEvaluatorId!.Value, row.MatchedCustomerPersonnelId!.Value,
                    row);

                _context.Evaluations.Add(evaluation);
                await _context.SaveChangesAsync();

                row.EvaluationId = evaluation.Id;
                row.StatusId = EvaluationImportRowStatuses.Ids.Imported;
                result.ImportedCount++;

                // Track newly imported CallId
                if (!string.IsNullOrWhiteSpace(row.ParsedCallId))
                    existingCallIds.Add(row.ParsedCallId);
            }
            catch (Exception ex)
            {
                result.FailedCount++;
                result.Errors.Add($"Satır {row.RowNumber}: {ex.Message}");
            }
        }

        await _context.SaveChangesAsync();
        await RecalculateSessionStats(sessionId);

        return result;
    }

    public async Task<List<CustomerPersonnelSearchDto>> SearchCustomerPersonnelAsync(int customerId, string query)
    {
        var q = query?.Trim().ToLower() ?? "";

        var filtered = _context.CustomerPersonnel
            .Where(cp => !cp.IsDeleted && cp.CustomerId == customerId
                && (cp.FirstName.ToLower().Contains(q) || cp.LastName.ToLower().Contains(q)
                    || (cp.FirstName + " " + cp.LastName).ToLower().Contains(q)))
            .OrderBy(cp => cp.FirstName).ThenBy(cp => cp.LastName)
            .Select(cp => new CustomerPersonnelSearchDto
            {
                Id = cp.Id,
                FullName = cp.FirstName + " " + cp.LastName,
            });

        // Arama yapılıyorsa limit koy, boş query'de tüm listeyi döndür
        if (!string.IsNullOrEmpty(q))
            filtered = (IOrderedQueryable<CustomerPersonnelSearchDto>)filtered.Take(20);

        return await filtered.ToListAsync();
    }

    public async Task<List<UserSearchDto>> SearchUsersAsync(string query)
    {
        var q = query?.Trim().ToLower() ?? "";

        var filtered = _context.Users
            .Where(u => !u.IsDeleted
                && (u.FirstName.ToLower().Contains(q) || u.LastName.ToLower().Contains(q)
                    || (u.FirstName + " " + u.LastName).ToLower().Contains(q)))
            .OrderBy(u => u.FirstName).ThenBy(u => u.LastName)
            .Select(u => new UserSearchDto
            {
                Id = u.Id,
                FullName = u.FirstName + " " + u.LastName,
            });

        if (!string.IsNullOrEmpty(q))
            filtered = (IOrderedQueryable<UserSearchDto>)filtered.Take(20);

        return await filtered.ToListAsync();
    }

    public async Task<List<ProjectSearchDto>> SearchProjectsAsync(int? customerId, string query)
    {
        var q = query?.Trim().ToLower() ?? "";

        var dbQuery = _context.Projects.Where(p => !p.IsDeleted);

        if (customerId.HasValue)
            dbQuery = dbQuery.Where(p => p.CustomerId == customerId.Value);

        var filtered = dbQuery.Where(p => p.Name.ToLower().Contains(q))
            .OrderBy(p => p.Name)
            .Select(p => new ProjectSearchDto
            {
                Id = p.Id,
                Name = p.Name,
                ChecklistId = p.ChecklistId,
            });

        // Arama yapılıyorsa limit koy, boş query'de tüm listeyi döndür
        if (!string.IsNullOrEmpty(q))
            filtered = (IOrderedQueryable<ProjectSearchDto>)filtered.Take(20);

        return await filtered.ToListAsync();
    }

    public async Task<List<CustomerSearchDto>> GetCustomersAsync()
    {
        return await _context.Customers
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.CompanyName)
            .Select(c => new CustomerSearchDto
            {
                Id = c.Id,
                CompanyName = c.CompanyName,
            })
            .ToListAsync();
    }

    public async Task<List<ChecklistSearchDto>> GetChecklistsAsync()
    {
        return await _context.Checklists
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new ChecklistSearchDto
            {
                Id = c.Id,
                Name = c.Name,
            })
            .ToListAsync();
    }

    // ===== Private Helpers =====

    private static string? ParseProjectName(string? cellG)
    {
        if (string.IsNullOrWhiteSpace(cellG)) return null;

        // Format: "97500190795 - B405 Misli Aralık2025 - BİLYONER"
        var parts = cellG.Split(" - ", StringSplitOptions.TrimEntries);
        if (parts.Length < 2) return cellG.Trim();

        var projectPart = parts[1]; // "B405 Misli Aralık2025"

        // Remove month+year suffix: Ocak-Aralık + 4-digit year
        var cleaned = Regex.Replace(projectPart,
            @"\s*(Ocak|Şubat|Mart|Nisan|Mayıs|Haziran|Temmuz|Ağustos|Eylül|Ekim|Kasım|Aralık)\d{4}$",
            "", RegexOptions.IgnoreCase).Trim();

        return NormalizeName(cleaned);
    }

    private static string? ParseEvaluatorName(string? cellD)
    {
        if (string.IsNullOrWhiteSpace(cellD)) return null;

        // Format: "Aylin İşleyen - Kalite Uzmanı"
        var parts = cellD.Split(" - ", StringSplitOptions.TrimEntries);
        return parts[0].Trim();
    }

    private static string NormalizeName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name)) return string.Empty;

        // Remove numbers, normalize spaces
        var normalized = Regex.Replace(name, @"\d+", "").Trim();
        normalized = Regex.Replace(normalized, @"\s+", " ");
        return normalized;
    }

    private static string GenerateUsername(string firstName, string lastName)
    {
        // Turkish char replacements
        var replacements = new Dictionary<char, char>
        {
            ['ç'] = 'c', ['Ç'] = 'c', ['ğ'] = 'g', ['Ğ'] = 'g',
            ['ı'] = 'i', ['İ'] = 'i', ['ö'] = 'o', ['Ö'] = 'o',
            ['ş'] = 's', ['Ş'] = 's', ['ü'] = 'u', ['Ü'] = 'u'
        };

        string Normalize(string s) =>
            new string(s.ToLowerInvariant()
                .Select(c => replacements.TryGetValue(c, out var r) ? r : c)
                .Where(c => char.IsLetterOrDigit(c) || c == '.')
                .ToArray());

        var first = Normalize(firstName.Trim());
        var last = Normalize(lastName.Trim());

        if (string.IsNullOrEmpty(first)) first = "user";
        return string.IsNullOrEmpty(last) ? first : $"{first}.{last}";
    }

    private static DateTime? ParseDate(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        // Try multiple formats
        string[] formats = { "dd.MM.yyyy", "d.M.yyyy", "yyyy-MM-dd", "dd/MM/yyyy", "d/M/yyyy" };
        if (DateTime.TryParseExact(value.Trim(), formats, CultureInfo.InvariantCulture,
            DateTimeStyles.None, out var date))
            return date;

        if (DateTime.TryParse(value.Trim(), CultureInfo.GetCultureInfo("tr-TR"), DateTimeStyles.None, out date))
            return date;

        return null;
    }

    private static DateTime? ParseDateTime(string? dateStr, string? timeStr)
    {
        var date = ParseDate(dateStr);
        if (date == null) return null;

        if (!string.IsNullOrWhiteSpace(timeStr) && TimeSpan.TryParse(timeStr.Trim(), out var time))
            return date.Value.Add(time);

        return date;
    }

    private static decimal? ParseDecimal(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;

        var cleaned = value.Trim().Replace(',', '.');
        if (decimal.TryParse(cleaned, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
            return result;

        return null;
    }

    private static string? GetCellValue(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return null;
        return cell.GetString()?.Trim();
    }

    private static DateTime? GetCellDateValue(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return null;

        // Try direct DateTime (Excel stores dates as numeric internally)
        if (cell.DataType == XLDataType.DateTime)
            return cell.GetDateTime();

        if (cell.DataType == XLDataType.Number)
        {
            try { return cell.GetDateTime(); }
            catch { /* not a date number */ }
        }

        // Fallback: parse as string
        var str = cell.GetString()?.Trim();
        return ParseDate(str);
    }

    private static string? GetCellTimeValue(IXLWorksheet ws, int row, int col)
    {
        var cell = ws.Cell(row, col);
        if (cell.IsEmpty()) return null;

        // Time stored as DateTime in Excel
        if (cell.DataType == XLDataType.DateTime || cell.DataType == XLDataType.Number)
        {
            try
            {
                var dt = cell.GetDateTime();
                return dt.ToString("HH:mm");
            }
            catch { /* not a time */ }
        }

        // Try TimeSpan
        if (cell.DataType == XLDataType.TimeSpan)
        {
            try
            {
                var ts = cell.GetTimeSpan();
                return $"{(int)ts.TotalHours:D2}:{ts.Minutes:D2}";
            }
            catch { /* fallback */ }
        }

        return cell.GetString()?.Trim();
    }

    private static Evaluation CreateEvaluation(int projectId, int checklistId,
        int evaluatorId, int customerPersonnelId, EvaluationImportPendingRow row)
    {
        return new Evaluation
        {
            ProjectId = projectId,
            ChecklistId = checklistId,
            EvaluatorId = evaluatorId,
            EvaluatedCustomerPersonnelId = customerPersonnelId,
            StatusId = EvaluationStatuses.Ids.Completed,
            TotalScore = row.ParsedScore,
            MaxScore = 100,
            ScorePercentage = row.ParsedScore,
            CallId = row.ParsedCallId,
            CallDate = row.ParsedCallDate,
            CallTime = row.ParsedCallTime,
            Duration = row.ParsedDuration,
            EvaluationComment = row.ParsedComment,
            CompletedAt = row.ParsedModifiedDate,
            CreatedAt = row.ParsedCreatedDate ?? TurkeyTime.Now,
        };
    }

    private void TrackUnmatchedItem(Dictionary<string, EvaluationImportUnmatchedItem> dict,
        int sessionId, int itemTypeId, string originalValue)
    {
        var key = $"{itemTypeId}:{originalValue.ToLowerInvariant()}";
        if (dict.TryGetValue(key, out var existing))
        {
            existing.AffectedRowCount++;
        }
        else
        {
            dict[key] = new EvaluationImportUnmatchedItem
            {
                ImportSessionId = sessionId,
                ItemTypeId = itemTypeId,
                OriginalValue = originalValue,
                AffectedRowCount = 1,
                CreatedAt = TurkeyTime.Now
            };
        }
    }

    private async Task UpdatePendingRowsForResolvedItem(EvaluationImportUnmatchedItem item, int resolvedEntityId)
    {
        if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Project)
        {
            var rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedProjectValue == item.OriginalValue)
                .ToListAsync();

            foreach (var r in rows)
            {
                r.MatchedProjectId = resolvedEntityId;
                r.UnmatchedProjectValue = null;
            }
        }
        else if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Evaluator)
        {
            var rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedEvaluatorValue == item.OriginalValue)
                .ToListAsync();

            foreach (var r in rows)
            {
                r.MatchedEvaluatorId = resolvedEntityId;
                r.UnmatchedEvaluatorValue = null;
            }
        }
        else if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.EvaluatedPerson)
        {
            var rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedPersonValue == item.OriginalValue)
                .ToListAsync();

            foreach (var r in rows)
            {
                r.MatchedCustomerPersonnelId = resolvedEntityId;
                r.UnmatchedPersonValue = null;
            }
        }
    }

    private async Task SkipPendingRowsForItem(EvaluationImportUnmatchedItem item)
    {
        List<EvaluationImportPendingRow> rows;

        if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Project)
        {
            rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedProjectValue == item.OriginalValue)
                .ToListAsync();
        }
        else if (item.ItemTypeId == EvaluationImportUnmatchedItemTypes.Ids.Evaluator)
        {
            rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedEvaluatorValue == item.OriginalValue)
                .ToListAsync();
        }
        else
        {
            rows = await _context.EvaluationImportPendingRows
                .Where(r => !r.IsDeleted
                    && r.ImportSessionId == item.ImportSessionId
                    && r.StatusId == EvaluationImportRowStatuses.Ids.Pending
                    && r.UnmatchedPersonValue == item.OriginalValue)
                .ToListAsync();
        }

        foreach (var r in rows)
        {
            r.StatusId = EvaluationImportRowStatuses.Ids.Skipped;
        }
    }

    private async Task RecalculateSessionStats(int sessionId)
    {
        var session = await _context.EvaluationImportSessions
            .FirstOrDefaultAsync(s => !s.IsDeleted && s.Id == sessionId);
        if (session == null) return;

        var rows = await _context.EvaluationImportPendingRows
            .Where(r => !r.IsDeleted && r.ImportSessionId == sessionId)
            .ToListAsync();

        session.ImportedRows = rows.Count(r => r.StatusId == EvaluationImportRowStatuses.Ids.Imported);
        session.SkippedRows = rows.Count(r => r.StatusId == EvaluationImportRowStatuses.Ids.Skipped);
        session.PendingRows = rows.Count(r => r.StatusId == EvaluationImportRowStatuses.Ids.Pending);

        var hasUnresolved = await _context.EvaluationImportUnmatchedItems
            .AnyAsync(u => !u.IsDeleted && u.ImportSessionId == sessionId && !u.ResolutionActionId.HasValue);

        if (session.PendingRows == 0 && !hasUnresolved)
            session.StatusId = EvaluationImportSessionStatuses.Ids.Completed;

        await _context.SaveChangesAsync();
    }

    private static EvaluationImportSessionDto MapToSessionDto(EvaluationImportSession session)
    {
        return new EvaluationImportSessionDto
        {
            Id = session.Id,
            CustomerId = session.CustomerId,
            CustomerName = session.Customer?.CompanyName,
            FileName = session.FileName,
            StatusId = session.StatusId,
            StatusName = EvaluationImportSessionStatuses.GetById(session.StatusId)?.Description,
            StatusBadgeClass = EvaluationImportSessionStatuses.GetById(session.StatusId)?.CssClass,
            TotalRows = session.TotalRows,
            ImportedRows = session.ImportedRows,
            PendingRows = session.PendingRows,
            SkippedRows = session.SkippedRows,
            Notes = session.Notes,
            UploadedByUserId = session.UploadedByUserId,
            UploadedByUserName = session.UploadedByUser != null
                ? $"{session.UploadedByUser.FirstName} {session.UploadedByUser.LastName}"
                : null,
            CreatedAt = session.CreatedAt,
        };
    }
}

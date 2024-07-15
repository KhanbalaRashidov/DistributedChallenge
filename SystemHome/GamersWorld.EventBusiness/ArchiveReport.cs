﻿using GamersWorld.Application.Contracts.Document;
using GamersWorld.Application.Contracts.Events;
using GamersWorld.Application.Contracts.Notification;
using GamersWorld.Domain.Constants;
using GamersWorld.Domain.Dtos;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace GamersWorld.EventBusiness;

public class ArchiveReport(ILogger<ArchiveReport> logger, IDocumentRepository documentRepository, IServiceProvider serviceProvider, INotificationService notificationService)
    : IEventDriver<ArchiveReportEvent>
{
    private readonly ILogger<ArchiveReport> _logger = logger;
    private readonly IDocumentRepository _documentRepository = documentRepository;
    private readonly IServiceProvider _serviceProvider = serviceProvider;
    private readonly INotificationService _notificationService = notificationService;

    public async Task Execute(ArchiveReportEvent appEvent)
    {
        var request = new Domain.Requests.DocumentReadRequest
        {
            DocumentId = appEvent.DocumentId,
        };
        var doc = await _documentRepository.ReadDocumentAsync(request);
        if (doc == null || doc.Content == null)
        {
            _logger.LogWarning("{DocumentId} content not found", appEvent.DocumentId);
            return;
        }
        var writeOperator = _serviceProvider.GetRequiredKeyedService<IDocumentWriter>(Names.FtpWriteService);

        var writeResponse = await writeOperator.SaveAsync(new Domain.Requests.DocumentSaveRequest
        {
            DocumentId = appEvent.DocumentId,
            Content = doc.Content,
        });
        if (writeResponse.StatusCode != Domain.Enums.StatusCode.DocumentUploaded)
        {
            _logger.LogWarning("{DocumentId} save operation failed", doc.DocumentId);
            return;
        }

        var deleteResult = await _documentRepository.DeleteDocumentByIdAsync(request);
        if (deleteResult == 1)
        {
            var notificationData = new ReportNotification
            {
                DocumentId = appEvent.DocumentId,
                Content = appEvent.Title,
                IsSuccess = true,
                Topic = Domain.Enums.NotificationTopic.Archive.ToString()
            };

            await _notificationService.PushToUserAsync(appEvent.ClientId, JsonSerializer.Serialize(notificationData));
            _logger.LogWarning("{Document} is archiving to target", appEvent.DocumentId);
        }
        else
        {
            _logger.LogWarning("{DocumentId} delete from db operation failed", doc.DocumentId);
        }
    }
}

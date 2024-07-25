﻿using GamersWorld.Application.Contracts.Data;
using GamersWorld.Application.Contracts.Document;
using GamersWorld.Application.Tasking;
using GamersWorld.Domain.Constants;
using GamersWorld.Domain.Requests;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GamersWorld.JobHost.Business
{
    public class ReportArchiver(IServiceProvider serviceProvider, IReportDataRepository reportDataRepository, IReportDocumentDataRepository reportDocumentDataRepository, ILogger<ReportArchiver> logger)
        : IJobAction
    {
        public async Task Execute()
        {
            logger.LogInformation("Archive the expired reports to ftp process started at: {ExecuteTime}", DateTime.Now);

            var documentWriter = serviceProvider.GetRequiredKeyedService<IDocumentWriter>(Names.FtpWriteService);            
            var documentIdList = await reportDataRepository.GetExpiredReportsAsync();
            foreach (var documentId in documentIdList)
            {
                var report = await reportDataRepository.ReadReportAsync(documentId);
                report.Archived = true;
                var updatedCount = await reportDataRepository.UpdateReportAsync(report);
                if (updatedCount == 1)
                {
                    var doc = await reportDocumentDataRepository.ReadDocumentAsync(documentId);
                    if (doc == null)
                    {
                        logger.LogWarning("{DocumentId} content not found", documentId);
                        continue;
                    }
                    else
                    {
                        var uploadResponse = await documentWriter.SaveAsync(
                            new ReportSaveRequest
                            {
                                DocumentId = documentId,
                                Content = doc.Content
                            });

                        if (uploadResponse.StatusCode != Domain.Enums.StatusCode.DocumentUploaded)
                        {
                            logger.LogError("Error on ftp upload operation.{StatusCode}", uploadResponse.StatusCode);
                        }
                    }
                }
            }

            logger.LogInformation("Archive the expired reports to ftp process has been completed at: {ExecuteTime}", DateTime.Now);
        }
    }
}

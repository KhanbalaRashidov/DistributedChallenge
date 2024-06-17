﻿using GamersWorld.Events;
using GamersWorld.SDK;
using Microsoft.Extensions.Logging;

namespace GamersWorld.EventBusiness;

public class DeleteReport(ILogger<DeleteReport> logger) : IEventDriver<ReportProcessCompletedEvent>
{
    private readonly ILogger<DeleteReport> _logger = logger;

    public async Task Execute(ReportProcessCompletedEvent appEvent)
    {
        //TODO@buraksenyurt Must implement Report Process Completed actions
        _logger.LogWarning("{CreatedReportId} is deleting from system", appEvent.CreatedReportId);        
    }
}

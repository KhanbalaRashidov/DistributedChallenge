﻿using GamersWorld.AppEvents;
using GamersWorld.Common.Messages.Responses;
using GamersWorld.SDK;
using Microsoft.Extensions.Logging;

namespace GamersWorld.AppEventBusiness;

/*
    Rapor karşı sistemden diğer sistemin Local Storage'ına indiğinde
    ReportIsHereEvent hazırlanıp kuyruğa bırakılmış oluyor. 

    Bu olay yakalanırsa aşağıdaki nesne fonksiyonundaki işlemler yapılacak.
*/
public class UsePreparedReport(ILogger<UsePreparedReport> logger) : IEventDriver<ReportIsHereEvent>
{
    private readonly ILogger<UsePreparedReport> _logger = logger;

    public async Task<BusinessResponse> Execute(ReportIsHereEvent appEvent)
    {
        //TODO@buraksenyurt Must implement Use Report steps

        _logger.LogInformation("{TraceId}, {CreatedReportId}", appEvent.TraceId, appEvent.CreatedReportId);

        // Dokümanı Local Storage'dan oku
        // E-posta ile gönder
        // raporun hazırlandığına dair bir bilgilendirme olayı(ReportProcessCompleted) hazırlayıp fırlat
        throw new NotImplementedException();
    }
}

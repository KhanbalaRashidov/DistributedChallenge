﻿using GamersWorld.Domain.Requests;
using GamersWorld.WebApp.Models;
using GamersWorld.WebApp.Services;
using Microsoft.AspNetCore.Mvc;

namespace GamersWorld.WebApp.Controllers;

public class ReportsController(ILogger<ReportsController> logger, MessengerServiceClient messengerServiceClient)
    : Controller
{
    private readonly ILogger<ReportsController> _logger = logger;
    private readonly MessengerServiceClient _messengerServiceClient = messengerServiceClient;

    public async Task<IActionResult> Index()
    {
        var employeeId = HttpContext.Session.GetString("EmployeeId");
        var token = HttpContext.Session.GetString("JWToken");

        if (string.IsNullOrEmpty(employeeId) || string.IsNullOrEmpty(token))
        {
            return RedirectToAction("Login", "Account");
        }

        _logger.LogInformation("Getting reports for {EmployeeId}", employeeId);

        var request = new GetReportsByEmployeeRequest
        {
            EmployeeId = employeeId
        };
        var reportDocuments = await _messengerServiceClient.GetReportDocumentsByEmployeeAsync(request);
        var reports = new List<ReportModel>();
        foreach (var reportDocument in reportDocuments)
        {
            reports.Add(new ReportModel
            {
                Id = reportDocument.ReportId,
                Title = reportDocument.Title,
                Expression = reportDocument.Expression,
                DocumentId = reportDocument.DocumentId,
                InsertTime = reportDocument.InsertTime,
                ExpireTime = reportDocument.ExpireTime
            });
        }

        var viewModel = new ReportViewModel
        {
            EmployeeId = employeeId,
            Reports = reports
        };

        return View(viewModel);
    }


    [HttpGet("Reports/Download")]
    public async Task<IActionResult> Download(string documentId)
    {
        var document = await _messengerServiceClient
            .GetReportDocumentByIdAsync(documentId);
        if (document?.Base64Content != null)
        {
            var content = Convert.FromBase64String(document.Base64Content);
            return File(content, "application/octet-stream", $"{documentId}.txt");
        }
        return NotFound();
    }

    [HttpGet("Reports/Delete")]
    public async Task<IActionResult> Delete(string documentId, string title, string employeeId)
    {
        var response = await _messengerServiceClient
            .DeleteDocumentByIdAsync(
            new DeleteReportRequest
            {
                DocumentId = documentId,
                EmployeeId = employeeId,
                Title = title
            });
        if (response.Status == Domain.Enums.Status.DeleteRequestAccepted)
        {
            TempData["Notification"] = "Document deleting request has been sent.";
            return RedirectToAction("Index");
        }

        TempData["Notification"] = "Failed to delete request!";
        return RedirectToAction("Index");
    }

    [HttpGet("Reports/Archive")]
    public async Task<IActionResult> Archive(string documentId, string title, string employeeId)
    {
        var response = await _messengerServiceClient
            .ArchiveDocumentByIdAsync(new ArchiveReportRequest
            {
                Title = title,
                DocumentId = documentId,
                EmployeeId = employeeId
            });
        if (response.Status == Domain.Enums.Status.Success)
        {
            TempData["Notification"] = "Document archiving request has been sent.";
            return RedirectToAction("Index");
        }

        TempData["Notification"] = "Failed the archive request!";
        return RedirectToAction("Index");
    }
}

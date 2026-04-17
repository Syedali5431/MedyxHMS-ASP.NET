using MedyxHMS.Controllers;
using MedyxHMS.Models;
using MedyxHMS.Services.Implementations;
using MedyxHMS.Tests.TestSupport;
using MedyxHMS.ViewModels;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace MedyxHMS.Tests.Integration;

public class PublicBookingApprovalBillingFlowTests
{
    [Fact]
    public async Task PublicBooking_ToApproval_ToAppointmentBillLinkage_ShouldPersistExpectedState()
    {
        var dbName = Guid.NewGuid().ToString("N");
        await using var context = TestDbContextFactory.Create(dbName);

        context.Departments.Add(ModelFactory.CreateDepartment());
        context.Doctors.Add(ModelFactory.CreateDoctor());
        await context.SaveChangesAsync();

        var siteController = new SiteController(context, NullLogger<SiteController>.Instance)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = BuildHttpContextWithSession()
            }
        };

        siteController.HttpContext.Session.SetInt32("PublicBookingCaptchaExpected", 4);

        var booking = new PublicBookingViewModel
        {
            PatientName = "Public User",
            Phone = "01700000000",
            Email = "public.user@example.com",
            Gender = "Male",
            Age = "30",
            DoctorId = 1,
            PreferredDate = DateTime.Today.AddDays(2),
            PreferredTimeStr = "10:00",
            Symptoms = "Fever",
            Notes = "First visit",
            CaptchaAnswer = "4",
            Website = string.Empty
        };

        var bookingResult = await siteController.BookAppointment(booking) as RedirectToActionResult;

        Assert.NotNull(bookingResult);
        Assert.Equal("BookingConfirmation", bookingResult!.ActionName);

        var requestIdObj = bookingResult.RouteValues!["requestId"];
        Assert.NotNull(requestIdObj);
        var requestId = Convert.ToInt32(requestIdObj);

        var request = await context.PublicAppointmentRequests.FindAsync(requestId);
        Assert.NotNull(request);
        Assert.Equal("Pending", request!.Status);

        var fakeNotificationService = new FakePublicBookingNotificationService();
        var cmsController = new CmsController(
            context,
            NullLogger<CmsController>.Instance,
            new FakeSettingService(),
            new FakeEmailNotificationProvider(),
            new FakeSmsNotificationProvider(),
            fakeNotificationService,
            new ExportService());
        cmsController.TempData = new TempDataDictionary(new DefaultHttpContext(), new FakeTempDataProvider());

        var approvalResult = await cmsController.UpdateRequestStatus(request.Id, "Confirmed", "Approved for slot", false);

        Assert.IsType<RedirectToActionResult>(approvalResult);

        var updated = await context.PublicAppointmentRequests.FindAsync(request.Id);
        Assert.NotNull(updated);
        Assert.Equal("Confirmed", updated!.Status);
        Assert.Contains(request.Id, fakeNotificationService.ConfirmedRequestIds);

        var appointmentService = new AppointmentService(context);
        var appointment = await appointmentService.CreateAppointmentAsync(new Appointment
        {
            PatientId = updated.PatientId,
            DoctorId = updated.DoctorId,
            AppointmentDate = updated.PreferredDate,
            AppointmentTime = updated.PreferredTime,
            AppointmentType = "OPD",
            Priority = "Normal",
            Symptoms = updated.Symptoms ?? string.Empty,
            Notes = updated.Notes ?? string.Empty,
            CreatedBy = "integration-test",
            UpdatedBy = "integration-test"
        });

        var billingService = new BillingService(context);
        var bill = await billingService.CreateBillAsync(new Bill
        {
            PatientId = updated.PatientId,
            AppointmentId = appointment.Id,
            BillDate = DateTime.UtcNow.Date,
            DueDate = DateTime.UtcNow.Date.AddDays(7),
            TotalAmount = 250m,
            BillType = "OPD",
            Notes = "Auto-linked from approved booking appointment",
            CreatedBy = "integration-test"
        });

        Assert.True(appointment.Id > 0);
        Assert.True(bill.Id > 0);
        Assert.Equal(appointment.Id, bill.AppointmentId);
        Assert.Equal(updated.PatientId, bill.PatientId);
    }

    private static HttpContext BuildHttpContextWithSession()
    {
        var context = new DefaultHttpContext();
        context.Session = new TestSession();
        return context;
    }
}

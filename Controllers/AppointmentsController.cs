
namespace Test_A;
using Microsoft.AspNetCore.Mvc;



[ApiController]
[Route("api/appointments")]
public class AppointmentsController : ControllerBase
{
    private readonly IAppointmentService _appointmentService;

    public AppointmentsController(IAppointmentService appointmentService)
    {
        _appointmentService = appointmentService;
    }
    [HttpGet("{id}")]
    public async Task<IActionResult> GetAppointment(int id)
    {
        try
        {
            var appointment = await _appointmentService.GetAppointmentAsync(id);
            return Ok(appointment);
        }
        catch (Exception ex)
        {
            return ex.Message switch
            {
                "Appointment not found" => NotFound(ex.Message),
                _ => StatusCode(500, "Internal server error")
            };
        }
    }
    
    [HttpPost]
    public async Task<IActionResult> AddAppointment([FromBody] NewAppointmentDto request)
    {
        try
        {
            await _appointmentService.AddAppointmentAsync(request);
            return Created("", null);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(ex.Message);
        }
        catch (InvalidOperationException ex)
        {
            return Conflict(ex.Message); 
        }
        catch (Exception ex)
        {
            return StatusCode(500, ex.Message);
        }
    }

}

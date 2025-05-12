namespace Test_A;

public interface IAppointmentService
{
    Task<AppointmentDto> GetAppointmentAsync(int id);
    Task AddAppointmentAsync(NewAppointmentDto request);
}
namespace Test_A;

public class AppointmentDto
{
    public DateTime Date { get; set; }
    public PatientDto Patient { get; set; } = null!;
    public DoctorDto Doctor { get; set; } = null!;
    public List<ServiceDto> Services { get; set; } = new();
}
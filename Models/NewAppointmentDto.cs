namespace Test_A;

public class NewAppointmentDto
{
    
    public int AppointmentId { get; set; }
    public int PatientId { get; set; }
    public string Pwz { get; set; } = null!;
    public List<NewServiceDto> Services { get; set; } = new();
}
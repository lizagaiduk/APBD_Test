using Microsoft.Data.SqlClient;
namespace Test_A;

public class AppointmentService : IAppointmentService
{
    private readonly string _connectionString;

    public AppointmentService(IConfiguration configuration)
    {
        _connectionString = configuration.GetConnectionString("DefaultConnection")!;
    }

    public async Task<AppointmentDto> GetAppointmentAsync(int id)
    {
        using var conn = new SqlConnection(_connectionString);
        await conn.OpenAsync();
        var mainCmd = new SqlCommand(@"
            SELECT 
                a.date,
                p.first_name, p.last_name, p.date_of_birth,
                d.doctor_id, d.PWZ
            FROM Appointment a
            JOIN Patient p ON p.patient_id = a.patient_id
            JOIN Doctor d ON d.doctor_id = a.doctor_id
            WHERE a.appointment_id = @id", conn);

        mainCmd.Parameters.AddWithValue("@id", id);

        using var reader = await mainCmd.ExecuteReaderAsync();
        if (!await reader.ReadAsync())
            throw new Exception("Appointment not found");

        var appointment = new AppointmentDto
        {
            Date = reader.GetDateTime(0),
            Patient = new PatientDto
            {
                FirstName = reader.GetString(1),
                LastName = reader.GetString(2),
                DateOfBirth = reader.GetDateTime(3)
            },
            Doctor = new DoctorDto
            {
                Id = reader.GetInt32(4),
                Pwz = reader.GetString(5)
            },
            Services = new List<ServiceDto>()
        };

        await reader.CloseAsync();
        var serviceCmd = new SqlCommand(@"
            SELECT s.name, aps.service_fee
            FROM Service s
            JOIN Appointment_Service aps ON aps.service_id = s.service_id
            WHERE aps.appointment_id = @id", conn); 

        serviceCmd.Parameters.AddWithValue("@id", id);

        using var serviceReader = await serviceCmd.ExecuteReaderAsync();
        while (await serviceReader.ReadAsync())
        {
            appointment.Services.Add(new ServiceDto
            {
                Name = serviceReader.GetString(0),
                Fee = serviceReader.GetDecimal(1)
            });
        }
        return appointment;
    }
    
    public async Task AddAppointmentAsync(NewAppointmentDto request)
{
    using var conn = new SqlConnection(_connectionString);
    await conn.OpenAsync();
    using var tran = conn.BeginTransaction();
    var checkCmd = new SqlCommand("SELECT 1 FROM Appointment WHERE appointment_id = @id", conn, tran);
    checkCmd.Parameters.AddWithValue("@id", request.AppointmentId);
    if (await checkCmd.ExecuteScalarAsync() is not null)
        throw new InvalidOperationException("Appointment with given ID already exists");
    var patientCmd = new SqlCommand("SELECT 1 FROM Patient WHERE patient_id = @id", conn, tran);
    patientCmd.Parameters.AddWithValue("@id", request.PatientId);
    if (await patientCmd.ExecuteScalarAsync() is null)
        throw new ArgumentException("Patient not found");
    var doctorCmd = new SqlCommand("SELECT doctor_id FROM Doctor WHERE PWZ = @pwz", conn, tran);
    doctorCmd.Parameters.AddWithValue("@pwz", request.Pwz);
    var doctorObj = await doctorCmd.ExecuteScalarAsync();
    if (doctorObj is null)
        throw new ArgumentException("Doctor not found");
    int doctorId = (int)doctorObj;
    var insertAppointmentCmd = new SqlCommand(@"
        INSERT INTO Appointment (appoitnment_id, patient_id, doctor_id, date)
        VALUES (@id, @patient, @doctor, GETDATE())", conn, tran);
    insertAppointmentCmd.Parameters.AddWithValue("@id", request.AppointmentId);
    insertAppointmentCmd.Parameters.AddWithValue("@patient", request.PatientId);
    insertAppointmentCmd.Parameters.AddWithValue("@doctor", doctorId);
    await insertAppointmentCmd.ExecuteNonQueryAsync();
    foreach (var service in request.Services)
    {
        var serviceIdCmd = new SqlCommand("SELECT service_id FROM Service WHERE name = @name", conn, tran);
        serviceIdCmd.Parameters.AddWithValue("@name", service.ServiceName);
        var serviceIdObj = await serviceIdCmd.ExecuteScalarAsync();
        if (serviceIdObj is null)
            throw new ArgumentException($"Service not found: {service.ServiceName}");
        int serviceId = (int)serviceIdObj;
        var insertServiceCmd = new SqlCommand(@"
            INSERT INTO Appointment_Service (appoitnment_id, service_id, service_fee)
            VALUES (@appid, @servid, @fee)", conn, tran);
        insertServiceCmd.Parameters.AddWithValue("@appid", request.AppointmentId);
        insertServiceCmd.Parameters.AddWithValue("@servid", serviceId);
        insertServiceCmd.Parameters.AddWithValue("@fee", service.ServiceFee);
        await insertServiceCmd.ExecuteNonQueryAsync();
    }
    await tran.CommitAsync();
}

}

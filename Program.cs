using Test_A;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddControllers();
builder.Services.AddScoped<IAppointmentService, AppointmentService>(); 



var app = builder.Build();

app.UseAuthorization();
app.MapControllers();
app.Run();
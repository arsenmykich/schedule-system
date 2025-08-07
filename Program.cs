using Microsoft.EntityFrameworkCore;
using ScheduleSystem.Data;
using ScheduleSystem.Services;
using System.Reflection;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();

// Configure Entity Framework with In-Memory database
builder.Services.AddDbContext<ScheduleDbContext>(options =>
    options.UseInMemoryDatabase("ScheduleSystemDb"));

// Register services
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IMeetingSchedulingService, MeetingSchedulingService>();

// Add API documentation
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Schedule System API",
        Version = "v1",
        Description = "A meeting scheduling system that finds the earliest available time slot for multiple users",
        Contact = new()
        {
            Name = "Schedule System",
            Email = "support@schedulesystem.com"
        }
    });

    // Include XML comments for better API documentation
    var xmlFilename = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFilename);
    if (File.Exists(xmlPath))
    {
        options.IncludeXmlComments(xmlPath);
    }
});

// Add CORS policy for development
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// Add logging
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.AddDebug();

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Schedule System API v1");
        options.RoutePrefix = string.Empty; // Serve Swagger at root
    });
}

app.UseHttpsRedirection();
app.UseCors();
app.UseAuthorization();
app.MapControllers();

// Seed some sample data for testing
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ScheduleDbContext>();
    await SeedDataAsync(context);
}

app.Run();

/// <summary>
/// Seeds the database with sample data for testing
/// </summary>
static async Task SeedDataAsync(ScheduleDbContext context)
{
    if (await context.Users.AnyAsync())
        return; // Data already seeded

    var users = new[]
    {
        new ScheduleSystem.Models.User { Name = "Alice Johnson" },
        new ScheduleSystem.Models.User { Name = "Bob Smith" },
        new ScheduleSystem.Models.User { Name = "Charlie Brown" },
        new ScheduleSystem.Models.User { Name = "Diana Prince" },
        new ScheduleSystem.Models.User { Name = "Eve Wilson" }
    };

    context.Users.AddRange(users);
    await context.SaveChangesAsync();

    // Add some sample meetings to create conflicts for testing
    var sampleMeetings = new[]
    {
        new ScheduleSystem.Models.Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(10), // 10:00 AM today
            EndTime = DateTime.UtcNow.Date.AddHours(11),   // 11:00 AM today
            ParticipantIds = new List<int> { 1, 2 },
            Participants = new List<ScheduleSystem.Models.User> { users[0], users[1] }
        },
        new ScheduleSystem.Models.Meeting
        {
            StartTime = DateTime.UtcNow.Date.AddHours(14), // 2:00 PM today
            EndTime = DateTime.UtcNow.Date.AddHours(15),   // 3:00 PM today
            ParticipantIds = new List<int> { 2, 3 },
            Participants = new List<ScheduleSystem.Models.User> { users[1], users[2] }
        }
    };

    context.Meetings.AddRange(sampleMeetings);
    await context.SaveChangesAsync();

    Console.WriteLine("Sample data seeded successfully!");
}
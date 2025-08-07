# Schedule System - Meeting Scheduling API

A robust ASP.NET Core Web API that intelligently schedules meetings for multiple users by finding the earliest available time slot without conflicts.

## 🚀 Features

- **Smart Scheduling Algorithm**: Finds the earliest available time slot that accommodates all participants
- **Conflict Detection**: Prevents double-booking and handles partial overlaps
- **Business Hours Enforcement**: Respects 9:00 AM - 5:00 PM business hours (UTC)
- **Clean Architecture**: Separation of concerns with proper layering
- **Comprehensive Testing**: Unit tests covering all edge cases
- **API Documentation**: Swagger/OpenAPI integration
- **In-Memory Database**: No external dependencies for quick testing

## 🏗️ Architecture

The project follows clean architecture principles:

```
ScheduleSystem/
├── Controllers/         # API endpoints
├── Services/           # Business logic layer
├── Models/            # Data models and DTOs
├── Data/              # Database context
└── Tests/             # Unit tests
```

### Key Components

- **MeetingSchedulingService**: Core algorithm implementation
- **UserService**: User management operations
- **ScheduleDbContext**: Entity Framework database context
- **Controllers**: RESTful API endpoints

## 📋 API Endpoints

### Users

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/users` | Create a new user |
| GET | `/users` | Get all users |
| GET | `/users/{id}` | Get user by ID |
| GET | `/users/{id}/meetings` | Get user's meetings |

### Meetings

| Method | Endpoint | Description |
|--------|----------|-------------|
| POST | `/meetings` | Schedule a new meeting |
| GET | `/meetings` | Get all meetings |
| GET | `/meetings/{id}` | Get meeting by ID |

## 🔧 Setup Instructions

### Prerequisites

- .NET 9.0 SDK
- Any IDE (Visual Studio, VS Code, JetBrains Rider)

### Installation

1. **Clone the repository**
   ```bash
   git clone <repository-url>
   cd ScheduleSystem
   ```

2. **Restore dependencies**
   ```bash
   dotnet restore
   ```

3. **Run the application**
   ```bash
   dotnet run
   ```

4. **Access the API**
   - Swagger UI: `https://localhost:7234` or `http://localhost:5100`
   - API Base URL: `https://localhost:7234`

### Running Tests

```bash
dotnet test
```

## 📝 Usage Examples

### 1. Create Users

```bash
curl -X POST "https://localhost:7234/users" \
  -H "Content-Type: application/json" \
  -d '{"name": "Alice Johnson"}'
```

### 2. Schedule a Meeting

```bash
curl -X POST "https://localhost:7234/meetings" \
  -H "Content-Type: application/json" \
  -d '{
    "participantIds": [1, 2, 3],
    "durationMinutes": 60,
    "earliestStart": "2025-01-20T09:00:00Z",
    "latestEnd": "2025-01-20T17:00:00Z"
  }'
```

### 3. Get User's Meetings

```bash
curl -X GET "https://localhost:7234/users/1/meetings"
```

## 🧮 Algorithm Details

The scheduling algorithm implements the following logic:

1. **Input Validation**: Verifies participants exist and time constraints are valid
2. **Conflict Detection**: Retrieves all existing meetings for participants in the time range
3. **Slot Merging**: Merges overlapping busy slots to optimize search
4. **Business Hours**: Ensures meetings fall within 9:00 AM - 5:00 PM UTC
5. **Gap Finding**: Identifies the earliest available time slot of sufficient duration

### Algorithm Complexity

- **Time Complexity**: O(n log n) where n is the number of existing meetings
- **Space Complexity**: O(n) for storing busy slots

### Edge Cases Handled

- ✅ Partial overlaps between meetings
- ✅ Back-to-back meetings
- ✅ No available time slots
- ✅ Business hours constraints
- ✅ Invalid time windows
- ✅ Multiple participant scheduling
- ✅ Overlapping and adjacent meetings

## 🧪 Testing

The project includes comprehensive unit tests covering:

- **Happy Path Scenarios**: Basic scheduling with no conflicts
- **Conflict Resolution**: Scheduling around existing meetings
- **Edge Cases**: Business hours, invalid inputs, no available slots
- **Algorithm Logic**: Overlap detection, slot merging, gap finding

### Test Coverage

- ✅ No conflicts scheduling
- ✅ Existing meeting conflicts
- ✅ Overlapping meeting handling
- ✅ Back-to-back meeting scenarios
- ✅ Business hours enforcement
- ✅ Invalid input validation
- ✅ Multiple participant coordination

## ⚙️ Configuration

### Business Hours

Default business hours are 9:00 AM to 5:00 PM UTC. These can be modified in:

```json
{
  "ScheduleSystem": {
    "BusinessHours": {
      "StartTime": "09:00",
      "EndTime": "17:00"
    }
  }
}
```

### Logging

Logging is configured in `appsettings.json` and `appsettings.Development.json`.

## 🚧 Known Limitations

1. **Single Day Scheduling**: Currently supports same-day meetings only
2. **UTC Timezone**: All times are in UTC (no timezone conversion)
3. **In-Memory Database**: Data doesn't persist between application restarts
4. **No Authentication**: No user authentication or authorization
5. **No Recurring Meetings**: Doesn't support recurring meeting patterns
6. **Fixed Business Hours**: Business hours are global, not per-user

## 🔮 Future Enhancements

- [ ] Multi-day meeting scheduling
- [ ] Timezone support
- [ ] Persistent database (SQL Server, PostgreSQL)
- [ ] User authentication and authorization
- [ ] Recurring meeting patterns
- [ ] Per-user business hours and availability
- [ ] Meeting reminders and notifications
- [ ] Calendar integration (Outlook, Google Calendar)
- [ ] Meeting room resource booking
- [ ] Attendee response tracking (Accept/Decline)

## 🛠️ Development

### Project Structure

```
ScheduleSystem/
├── Controllers/
│   ├── MeetingsController.cs
│   └── UsersController.cs
├── Data/
│   └── ScheduleDbContext.cs
├── Models/
│   ├── User.cs
│   ├── Meeting.cs
│   ├── CreateUserRequest.cs
│   ├── CreateMeetingRequest.cs
│   └── MeetingResponse.cs
├── Services/
│   ├── IUserService.cs
│   ├── UserService.cs
│   ├── IMeetingSchedulingService.cs
│   └── MeetingSchedulingService.cs
├── Tests/
│   └── MeetingSchedulingServiceTests.cs
├── Program.cs
├── ScheduleSystem.csproj
└── README.md
```

### Key Design Decisions

1. **Clean Architecture**: Separated concerns into controllers, services, and data layers
2. **Dependency Injection**: Used built-in ASP.NET Core DI container
3. **Entity Framework**: Used EF Core with in-memory database for simplicity
4. **Algorithm Efficiency**: Implemented O(n log n) scheduling algorithm
5. **Comprehensive Testing**: Created extensive unit tests for reliability

## 📄 License

This project is licensed under the MIT License - see the LICENSE file for details.

## 🤝 Contributing

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Add tests for new functionality
5. Ensure all tests pass
6. Submit a pull request

---

**Total Development Time**: ~2-3 hours as requested

**Contact**: For questions or support, please open an issue in the repository.
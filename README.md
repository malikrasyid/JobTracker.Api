# JobTracker API

A .NET API for tracking job applications and managing application pipelines.

## Prerequisites

- .NET SDK 9.0 or later
- MongoDB database (local or MongoDB Atlas)

## Configuration

The API uses environment variables for configuration. Create a `.env` file in the project root:

```env
# Database
MONGO_URI=your_mongodb_connection_string
MONGO_DBNAME=your_database_name

# JWT Configuration
JWT_SECRET=your_jwt_secret_key
JWT_ISSUER=JobTrackerApi
JWT_AUDIENCE=JobTrackerClient
```

Note: The JWT secret must be at least 16 characters long (128 bits) when using HS256. If the secret is shorter you'll get an error when creating tokens.

⚠️ **Security Note**: Never commit `.env` to version control. Add it to `.gitignore`:
```bash
echo ".env" >> .gitignore
```

For development, you can also use .NET User Secrets:
```bash
dotnet user-secrets init
dotnet user-secrets set "JWT_SECRET" "your_development_secret"
dotnet user-secrets set "MONGO_URI" "your_development_connection_string"
```

## Running the API

1. Restore packages:
   ```bash
   dotnet restore
   ```

2. Build the project:
   ```bash
   dotnet build
   ```

3. Run the API:
   ```bash
   dotnet run
   ```

The API will start on:
- http://localhost:5027 (HTTP)
- https://localhost:7112 (HTTPS, if configured)

## API Endpoints

The API provides endpoints for:
- Authentication (`/api/auth`)
- Job Applications (`/api/jobs`)
- Application Pipelines (`/api/pipelines`)

## Development

### Package Dependencies
- BCrypt.Net-Next - Password hashing
- dotenv.net - Environment variable loading
- Microsoft.AspNetCore.Authentication.JwtBearer - JWT authentication
- MongoDB.Driver - MongoDB database access
- System.IdentityModel.Tokens.Jwt - JWT token handling
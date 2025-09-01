# Zenko

This project includes a simple login and registration system backed by a SQL Server database.

## Running the app

1. Update the `DefaultConnection` string in `appsettings.json` to point to your SQL Server instance.
2. Run database script `Zenko.sql` to create required tables, including `Usuarios`.
3. Start the application with:
   
   ```bash
   dotnet run
   ```
4. Navigate to `/Account/Login` to sign in or create a new account.

New registrations are stored in the database after verifying the username is unique.

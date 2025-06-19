using System;
using System.IO;
using System.Net.Sockets;
using MySql.Data.MySqlClient;
Boolean debug = true;
// MySQL connection string
string connectionString = "Server=localhost;Database=UserManagementDB;Uid=root;Pwd=L3tM31n;";
if (args.Length == 0)
{
    Console.WriteLine("Starting Server");
    RunServer();
}
else
{
    for (int i = 0; i < args.Length; i++)
    {
        ProcessCommand(args[i]);
    }
}
void ProcessCommand(string command)
{
    if (debug) Console.WriteLine($"\nCommand: {command}");
    try
    {
        string[] slice = command.Split(new char[] { '?' }, 2);
        string loginID = slice[0];
        string operation = null;
        string update = null;
        string field = null;
        if (slice.Length == 2)
        {
            operation = slice[1];
            string[] pieces = operation.Split(new char[] { '=' }, 2);
            field = pieces[0];
            if (pieces.Length == 2) update = pieces[1];
        }
        if (debug) Console.Write($"Operation on LoginID '{loginID}'");
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();
            if (command.Contains("?"))
            {
                if (command.Contains("="))
                {
                    // This is an update command
                    UpdateField(command, connection);
                }
                else
                {
                    // This is a field lookup command
                    LookupField(command, connection);
                }
            }
            else
            {
                // This is a full dump command
                Dump(command, connection);
            }
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Fault in Command Processing: {e}");
    }
}
void Dump(string loginID, MySqlConnection connection)
{
    string query = @"
SELECT Users.UserID, Surname, Forenames, Title, Location, Positions.PositionName,
Phones.PhoneNumber, Emails.EmailAddress
FROM Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
JOIN UserPositions ON Users.UserID = UserPositions.UserID
JOIN Positions ON UserPositions.PositionID = Positions.PositionID
JOIN UserPhones ON Users.UserID = UserPhones.UserID
JOIN Phones ON UserPhones.PhoneID = Phones.PhoneID
JOIN UserEmails ON Users.UserID = UserEmails.UserID
JOIN Emails ON UserEmails.EmailID = Emails.EmailID
WHERE LoginAccounts.LoginID = @LoginID;";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@LoginID", loginID);
        using (var reader = command.ExecuteReader())
        {
            if (reader.Read())
            {
                Console.WriteLine("Dumping fields for LoginID: " + loginID);
                Console.WriteLine($"UserID: {reader["UserID"]}");
                Console.WriteLine($"Surname: {reader["Surname"]}");
                Console.WriteLine($"Forenames: {reader["Forenames"]}");
                Console.WriteLine($"Title: {reader["Title"]}");
                Console.WriteLine($"Location: {reader["Location"]}");
                Console.WriteLine($"Position: {reader["PositionName"]}");
                Console.WriteLine($"Phone: {reader["PhoneNumber"]}");
                Console.WriteLine($"Email: {reader["EmailAddress"]}");
            }
            else
            {
                Console.WriteLine($"No records found for LoginID: {loginID}");
            }
        }
    }
}
void LookupField(string command, MySqlConnection connection)
{
    var parts = command.Split('?');
    string loginID = parts[0];
    string field = parts[1];
    string query = $@"
SELECT {field} FROM Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
WHERE LoginAccounts.LoginID = @LoginID;";
    using (var sqlCommand = new MySqlCommand(query, connection))
    {
        sqlCommand.Parameters.AddWithValue("@LoginID", loginID);
        try
        {
            var result = sqlCommand.ExecuteScalar();
            if (result != null)
            {
                Console.WriteLine($"{field} for LoginID {loginID}: {result}");
            }
            else
            {
                Console.WriteLine($"No value found for {field} for LoginID: {loginID}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Invalid field: {field}. Error: {ex.Message}");
        }
    }
}
void LookupLocation(string loginID, MySqlConnection connection)
{
    string query = @"
SELECT Location
FROM Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
WHERE LoginAccounts.LoginID = @LoginID;";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@LoginID", loginID);
        try
        {
            var result = command.ExecuteScalar();
            if (result != null)
            {
                Console.WriteLine($"Location for LoginID {loginID}: {result}");
            }
            else
            {
                Console.WriteLine($"LoginID '{loginID}' not found.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error retrieving location for LoginID '{loginID}'. Error: {ex.Message}");
        }
    }
}
void UpdateField(string command, MySqlConnection connection)
{
    var parts = command.Split(new[] { "?", "=" }, StringSplitOptions.RemoveEmptyEntries);
    if (parts.Length != 3)
    {
        Console.WriteLine($"Invalid update command: {command}");
        return;
    }
    string loginID = parts[0];
    string field = parts[1];
    string newValue = parts[2];
    string query = $@"
UPDATE Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
SET {field} = @NewValue
WHERE LoginAccounts.LoginID = @LoginID;";
    using (var sqlCommand = new MySqlCommand(query, connection))
    {
        sqlCommand.Parameters.AddWithValue("@NewValue", newValue);
        sqlCommand.Parameters.AddWithValue("@LoginID", loginID);
        try
        {
            int rowsAffected = sqlCommand.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                Console.WriteLine($"Updated {field} for LoginID {loginID} to {newValue}");
            }
            else
            {
                Console.WriteLine($"No records updated for LoginID: {loginID}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating field: {field}. Error: {ex.Message}");
        }
    }
}
void UpdateLocation(string loginID, string update, MySqlConnection connection)
{
    string query = @"
UPDATE Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
SET Location = @UpdateValue
WHERE LoginAccounts.LoginID = @LoginID;";
    using (var command = new MySqlCommand(query, connection))
    {
        command.Parameters.AddWithValue("@UpdateValue", update);
        command.Parameters.AddWithValue("@LoginID", loginID);
        try
        {
            int rowsAffected = command.ExecuteNonQuery();
            if (rowsAffected > 0)
            {
                Console.WriteLine($"Updated Location for LoginID '{loginID}' to '{update}'.");
            }
            else
            {
                Console.WriteLine($"No records updated for LoginID '{loginID}'.");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error updating location for LoginID '{loginID}'. Error: {ex.Message}");
        }
    }
}
void RunServer()
{
    TcpListener listener = new TcpListener(443);
    try
    {
        listener.Start();
        Console.WriteLine("Server is running...");
        while (true)
        {
            Socket connection = listener.AcceptSocket();
            using (var stream = new NetworkStream(connection))
            {
                doRequest(stream);
            }
            connection.Close();
        }
    }
    catch (Exception e)
    {
        Console.WriteLine($"Server Error: {e}");
    }
}
void doRequest(NetworkStream socketStream)
{
    using (var reader = new StreamReader(socketStream))
    using (var writer = new StreamWriter(socketStream))
    {
        string command = reader.ReadLine();
        Console.WriteLine($"Received Network Command: '{command}'");
        if (command.StartsWith("GET /?name=") && command.EndsWith(" HTTP/1.1"))
        {
            // Lookup Location
            string loginID = command.Split(" ")[1].Substring(7); // Extract LoginID
            string location = null; // Variable to hold the location
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                string query = @"
SELECT Location
FROM Users
JOIN LoginAccounts ON Users.UserID = LoginAccounts.UserID
WHERE LoginAccounts.LoginID = @LoginID;";
                using (var loginquerycommand = new MySqlCommand(query, connection))
                {
                    loginquerycommand.Parameters.AddWithValue("@LoginID", loginID);
                    var result = loginquerycommand.ExecuteScalar();
                    if (result != null)
                    {
                        location = result.ToString(); // Store the location
                    }
                }
            }
            if (!string.IsNullOrEmpty(location))
            {
                writer.WriteLine("HTTP/1.1 200 OK");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine($"In {location}.");
            }
            else
            {
                writer.WriteLine("HTTP/1.1 404 Not Found");
                writer.WriteLine("Content-Type: text/plain");
                writer.WriteLine();
                writer.WriteLine($"Location for {loginID} not found.");
            }
        }
        else if (command.StartsWith("POST / HTTP/1.1"))
        {
            // Update Location
            int contentLength = 0;
            string line;
            while ((line = reader.ReadLine()) != "")
            {
                if (line.StartsWith("Content-Length: "))
                {
                    contentLength = int.Parse(line.Substring(16));
                }
            }
            char[] body = new char[contentLength];
            reader.Read(body, 0, contentLength);
            string[] parts = new string(body).Split('&');
            string loginID = parts[0].Split('=')[1];
            string newLocation = parts[1].Split('=')[1];
            using (var connection = new MySqlConnection(connectionString))
            {
                connection.Open();
                UpdateLocation(loginID, newLocation, connection);
            }
            writer.WriteLine("HTTP/1.1 200 OK");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine();
            writer.WriteLine($"Location for {loginID} updated to {newLocation}.");
        }
        else
        {
            writer.WriteLine("HTTP/1.1 400 Bad Request");
            writer.WriteLine("Content-Type: text/plain");
            writer.WriteLine();
        }
        writer.Flush();
    }
}

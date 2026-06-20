# BarrelMonkeyApi

This project is a simple REST API built using ASP.NET Core and .NET 10. It allows users to manage Barrels and Monkeys and demonstrates CRUD operations, file-based data storage, logging, authentication, and Swagger documentation.

## Prerequisites

Make sure you have the .NET 10 SDK installed on your machine.

You can verify the installation by running:

bash
dotnet --version

If the SDK is installed correctly, you should see a version starting with 10.

## Running the Application

Navigate to the project directory and run:

bash
dotnet run


The application will start on:

```text
http://localhost:5080
```

Swagger UI is available at:

```text
http://localhost:5080/swagger
```

You can use Swagger to test all available API endpoints.

## API Endpoints

### Barrel Endpoints

* GET /api/barrels - Get all barrels
* GET /api/barrels/{id} - Get a barrel by ID
* GET /api/barrels/{id}/monkeys - Get all monkeys in a barrel
* POST /api/barrels - Create a new barrel
* PUT /api/barrels/{id} - Update a barrel
* DELETE /api/barrels/{id} - Delete a barrel

### Monkey Endpoints

* GET /api/monkeys - Get all monkeys
* GET /api/monkeys/{id} - Get a monkey by ID
* POST /api/monkeys - Create a new monkey
* PUT /api/monkeys/{id} - Update a monkey
* DELETE /api/monkeys/{id} - Delete a monkey

### Health Check

* GET /health - Verify that the service is running

## Authentication

Authentication is disabled in Development mode to make local testing easier.

For environments where authentication is enabled, include the following header in requests:

```text
X-Api-Key: my-secret-api-key
```

## Data Storage

This application does not use a database.

Data is stored locally in JSON files located in the Data folder:

```text
data/
  barrels.json
  monkeys.json
```

The files are created automatically when data is added.

## Assumptions

* A monkey can exist without being assigned to a barrel.
* Deleting a barrel does not delete its monkeys. Any monkeys assigned to that barrel become unassigned.
* Update operations support partial updates of fields.
* Authentication is disabled during local development for easier testing.

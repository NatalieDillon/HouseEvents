using HouseEvents.Data;
using HouseEvents.Data.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace HouseEvents.Api
{
    public class Program
	{
		public static void Main(string[] args)
		{
			var builder = WebApplication.CreateBuilder(args);

			// Add services to the container.
			builder.Services.AddAuthorization();
			builder.Services.AddProblemDetails();

			var app = builder.Build();

			// Get connection string
			string connectionString = app.Configuration["ConnectionStrings:HouseEventDb"] ?? string.Empty;
			app.Logger.Log(LogLevel.Information, "Connection string: {connectionString}", connectionString);
			
			// Configure the HTTP request pipeline.
			app.UseHttpsRedirection();

			app.UseAuthorization();

			// Creates class to interact with the database
			HouseEventsDB db = new(connectionString);

			// Creates the api interface
			app.MapGet("/", GetHouses);
			app.MapGet("/house", GetHouses);
			async Task<IResult> GetHouses() 
			{
				List<HouseDto> houses = await db.GetHouseInfoAsync();
				return Results.Ok(houses);
			};
						
			app.MapGet("/eventNoFixtures", async () =>
			{
				List<EventNoFixturesDto> dto = await db.GetEventNoFixturesAsync();
				return Results.Ok(dto);
			});

			app.MapGet("/eventNoFixtures/{eventId}", async (int eventId) =>
			{
				EventNoFixturesDto? dto = await db.GetEventNoFixturesAsync(eventId);
				IResult result = dto == null ? Results.NotFound() : Results.Ok(dto);
				return result;
			});

			app.MapGet("/house/{houseName}", async (string houseName) =>
			{
				HouseDto? house = await db.GetHouseInfoAsync(houseName);
				IResult result = house == null ? Results.NotFound() : Results.Ok(house);
				return result;
			});

			app.MapPut("/house/{houseName}/coordinator", async (string houseName, [FromQuery(Name = "coord")] string? coordinator) =>
			{
				await db.UpdateEventsCoordinatorAsync(houseName, coordinator);
			});

			app.MapPut("/participant/{participantId}", async (int participantId, [FromQuery(Name = "student")] string? studentName) =>
			{
				await db.UpdateEventParticipantAsync(participantId, studentName);
			});

			app.MapPost("/newEventNoFixtures", async (NewEventNoFixturesDto dto) =>
			{
				int eventId = await db.InsertEventNoFixturesAsync(dto);
				return Results.Ok(eventId);
			});

			app.Run();
		}
	}
}
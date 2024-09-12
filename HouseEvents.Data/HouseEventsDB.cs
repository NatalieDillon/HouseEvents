using HouseEvents.Data.Dtos;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using System.Data;
using static Azure.Core.HttpHeader;

namespace HouseEvents.Data
{
	public class HouseEventsDB
	{
		private readonly string _connectionString;

		public HouseEventsDB(string connectionString)
		{
			_connectionString = connectionString;
		}

		// All database calls are being made asynchronously
		// This is important as a web service will service many requests but will have a limited number of threads to use
		// This means that while the calls are being made threads can be used on other calls
		public async Task<List<HouseDto>> GetHouseInfoAsync()
		{
			List<HouseDto> result = new ();
			using (SqlConnection connection = new (_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT HouseName, UndermasterFirstName, EventsCoordinator from dbo.House";
				SqlDataReader reader = await cmd.ExecuteReaderAsync();
				while (reader.Read())
				{
					result.Add(GetHouse(reader));
				}
			}
			return result;
		}

		public async Task<List<EventNoFixturesDto>> GetEventNoFixturesAsync()
		{
			List<EventNoFixturesDto> result = new();
			using (SqlConnection connection = new(_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "select EventId, EventName, EventDetailId, EventDate, EventStartTime, EventEndTime, EventVenue, Notes, " +
					"HouseId, HouseName, Points, EventParticipantId, YearGroup, Reserve, StudentName, NoShow from " +
					"[dbo].[vwEventParticipantsNoFixture] order by EventId, HouseId, Reserve, YearGroup";
				SqlDataReader reader = await cmd.ExecuteReaderAsync();
				if (reader.HasRows)
				{
					result = GetEventsNoFixtures(reader);
				}
			}
			return result;
		}

		public async Task<EventNoFixturesDto?> GetEventNoFixturesAsync(int eventId)
		{
			EventNoFixturesDto? result = null;
			using (SqlConnection connection = new(_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "select EventId, EventName, EventDetailId, EventDate, EventStartTime, EventEndTime, EventVenue, Notes, " +
					"HouseId, HouseName, Points, EventParticipantId, YearGroup, Reserve, StudentName, NoShow from " +
					"[dbo].[vwEventParticipantsNoFixture] where EventId = @EventId order by EventId, HouseId, Reserve, YearGroup";
				cmd.Parameters.Add(new SqlParameter("@EventId", eventId));
				SqlDataReader reader = await cmd.ExecuteReaderAsync();
				int index = 0;
				if (reader.HasRows)
				{
					var dataTable = new DataTable();
					dataTable.Load(reader);
					result = GetEventNoFixtures(dataTable.Rows, ref index);
				}
			}
			return result;
		}

		public async Task<HouseDto?> GetHouseInfoAsync(string houseName)
		{
			HouseDto? result = null;
			using (SqlConnection connection = new (_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "SELECT HouseName, UndermasterFirstName, EventsCoordinator from dbo.House where HouseName = @houseName";
				cmd.Parameters.Add(new SqlParameter("@HouseName", houseName));
				SqlDataReader reader = await cmd.ExecuteReaderAsync();
				if (reader.Read())
				{
					result = GetHouse(reader);
				}
			}
			return result;
		}

		public async Task UpdateEventsCoordinatorAsync(string houseName, string? eventsCoordinator)
		{
			using (SqlConnection connection = new (_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "UPDATE dbo.House SET EventsCoordinator = @EventsCoordinator where HouseName = @HouseName";
				if (string.IsNullOrWhiteSpace(eventsCoordinator))
				{
					cmd.Parameters.Add(new SqlParameter("@EventsCoordinator", DBNull.Value));
				}
				else
				{
					cmd.Parameters.Add(new SqlParameter("@EventsCoordinator", eventsCoordinator));
				}
				cmd.Parameters.Add(new SqlParameter("@HouseName", houseName));
				await cmd.ExecuteNonQueryAsync();
			}
		}

		public async Task UpdateEventParticipantAsync(int participantId, string? studentName)
		{
			using (SqlConnection connection = new(_connectionString))
			{
				connection.Open();
				SqlCommand cmd = connection.CreateCommand();
				cmd.CommandText = "UPDATE dbo.EventParticipant SET StudentName = @StudentName where EventParticipantId = @EventParticipantId";
				cmd.Parameters.Add(new SqlParameter("@EventParticipantId", participantId));
				if (string.IsNullOrWhiteSpace(studentName))
				{
					cmd.Parameters.Add(new SqlParameter("@StudentName", DBNull.Value));
				}
				else
				{
					cmd.Parameters.Add(new SqlParameter("@StudentName", studentName));
				}
				
				await cmd.ExecuteNonQueryAsync();
			}
		}

		public async Task<int> InsertEventNoFixturesAsync(NewEventNoFixturesDto dto)
		{
			int eventId = 0;
			using (SqlConnection connection = new (_connectionString))
			{
				connection.Open();
				SqlTransaction sqlTransaction = connection.BeginTransaction();
				try
				{
					eventId = await InsertEventAsync(connection, sqlTransaction, dto.EventName, dto.EventDate);
					int eventDetailId = await InsertEventDetailAsync(connection, sqlTransaction, eventId, dto);
					await InsertHouseEventAsync(connection, sqlTransaction, eventDetailId);
					
					List<Task> tasks = new ();
					foreach (ParticipantDetailDto item in dto.Participants)
					{
						tasks.Add(InsertParticipantAsync(connection, sqlTransaction, eventDetailId, item));
					}
					Task.WaitAll(tasks.ToArray());
					sqlTransaction.Commit();
				}
				catch (SqlException ex) 
				{ 
					sqlTransaction.Rollback();
					throw ex;				
				}
			}
			return eventId;
		}

		private static async Task InsertParticipantAsync(SqlConnection connection, SqlTransaction transaction, int eventDetailId, ParticipantDetailDto dto)
		{
			SqlCommand cmd = connection.CreateCommand();
			cmd.Transaction = transaction;
			cmd.CommandText = "INSERT INTO dbo.EventParticipant(HouseEventId, YearGroup, Reserve) " +
			"SELECT HouseEventId, @YearGroup, @IsReserve FROM dbo.HouseEvent WHERE eventDetailId = @EventDetailId";
			for (int i = 2; i <= dto.NumberRequired; i++)
			{
				cmd.CommandText += "UNION " +
					"SELECT HouseEventId, @YearGroup, @IsReserve FROM dbo.HouseEvent WHERE eventDetailId = @EventDetailId";
			}
			cmd.Parameters.Add(new SqlParameter("@EventDetailId", eventDetailId));
			cmd.Parameters.Add(new SqlParameter("@YearGroup", dto.AllowableYearGroups));
			cmd.Parameters.Add(new SqlParameter("@IsReserve", dto.Reserve));
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task InsertHouseEventAsync(SqlConnection connection, SqlTransaction transaction, int eventDetailId)
		{
			SqlCommand cmd = connection.CreateCommand();
			cmd.Transaction = transaction;
			cmd.CommandText = "INSERT INTO dbo.[HouseEvent](EventDetailID, HouseID, Points) " +
				"select @EventDetailId, houseId, 0 from dbo.House ";
			cmd.Parameters.Add(new SqlParameter("@EventDetailId", eventDetailId));
			await cmd.ExecuteNonQueryAsync();
		}

		private static async Task<int> InsertEventDetailAsync(SqlConnection connection, SqlTransaction transaction, int eventId, NewEventNoFixturesDto dto)
		{
			SqlCommand cmd = connection.CreateCommand();
			cmd.Transaction = transaction;
			cmd.CommandText = "INSERT INTO dbo.[EventDetail](EventID, EventDate, EventStartTime, EventEndTime, EventVenue, Notes) " +
				"OUTPUT INSERTED.EventDetailID " +
				"values(@EventID, @EventDate, @EventStartTime, @EventEndTime, @EventVenue, @Notes) ";				
			cmd.Parameters.Add(new SqlParameter("@EventId", eventId));
			cmd.Parameters.Add(new SqlParameter("@EventDate", dto.EventDate));
			cmd.Parameters.Add(new SqlParameter("@EventStartTime", dto.EventEndTime));
			cmd.Parameters.Add(new SqlParameter("@EventEndTime", dto.EventStartTime));
			cmd.Parameters.Add(new SqlParameter("@EventVenue", dto.Venue));
			cmd.Parameters.Add(new SqlParameter("@Notes", dto.Notes));
			var result = await cmd.ExecuteScalarAsync();
			int eventDetailId = Convert.ToInt32(result);
			return eventDetailId;
		}

		private static async Task<int> InsertEventAsync(SqlConnection connection, SqlTransaction transaction, string eventName, DateOnly eventDate)
		{
			SqlCommand cmd = connection.CreateCommand();
			cmd.Transaction = transaction;
			cmd.CommandText = "INSERT INTO dbo.[Event](EventName, SchoolYear) OUTPUT INSERTED.EventID  VALUES (@EventName, @EventSchoolYear) ";
			cmd.Parameters.Add(new SqlParameter("@EventName", eventName));
			cmd.Parameters.Add(new SqlParameter("@EventSchoolYear", GetSchoolYear(eventDate)));
			var result = await cmd.ExecuteScalarAsync();
			int eventId = Convert.ToInt32(result);
			return eventId;
		}

		private static int GetSchoolYear(DateOnly eventDate)
		{
			int ret = eventDate.Year;
			if (eventDate.Month < 9) {

				ret -= 1;
			}
			return ret;
		}

		private static HouseDto GetHouse(SqlDataReader reader)
		{
			return new HouseDto(reader.GetString(0), reader.GetString(1), GetNullableString(reader.GetValue(2)));
		}

		private static string? GetNullableString(object obj)
		{
			string? ret = null;
			if (obj is not DBNull)
			{
				ret = (string)obj;
			}
			return ret;
		}

		private static bool? GetNullableBoolean(object obj)
		{
			bool? ret = null;
			if (obj is not DBNull)
			{
				ret = (bool)obj;
			}
			return ret;
		}


		private static EventNoFixturesDto GetEventNoFixtures(DataRowCollection rows, ref int index)
		{
			DataRow row = rows[index];
			
			EventNoFixturesDto eventDto = new(row.Field<int>(0), row.Field<string>(1) ?? string.Empty, row.Field<int>(2),
				DateOnly.FromDateTime(row.Field<DateTime>(3)),  TimeOnly.FromTimeSpan(row.Field<TimeSpan>(4)), 
				TimeOnly.FromTimeSpan(row.Field<TimeSpan>(5)), GetNullableString(row[6]),
				GetNullableString(row[7]));

			HouseEventDto houseEventDto = new(row.Field<string>(9) ?? string.Empty, row.Field<int>(10));
			eventDto.Houses.Add(houseEventDto);

			do
			{
				row = rows[index];

				if (houseEventDto.HouseName != row.Field<string>(9))
				{
					houseEventDto = new(row.Field<string>(9) ?? string.Empty, row.Field<int>(10));
					eventDto.Houses.Add(houseEventDto);
				}

				ParticipantDto participant = new(row.Field<int>(11), GetNullableString(row[14]), row.Field<bool>(13), row.Field<string>(12) ?? string.Empty, GetNullableBoolean(row[15]));
				houseEventDto.Participants.Add(participant);

				index += 1;				

			} while (index < rows.Count  && eventDto.EventId == row.Field<int>(0));

			return eventDto;
		}
		private static List<EventNoFixturesDto> GetEventsNoFixtures(SqlDataReader reader)
		{
			// Probably best to use Entity framework for this type of thing
			// But illustrating how to do it without the use of frameworks. It's quite painful.
			var dataTable = new DataTable();
			dataTable.Load(reader);
			List<EventNoFixturesDto> result = new ();
			int index = 0;
			do
			{
				var eventDto = GetEventNoFixtures(dataTable.Rows, ref index);
				result.Add(eventDto);

			} while (index < dataTable.Rows.Count);

			return result;
		}

		
	}
}
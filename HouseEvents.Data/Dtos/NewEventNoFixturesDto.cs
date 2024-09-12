using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseEvents.Data.Dtos
{
	public class NewEventNoFixturesDto
	{
		public string EventName { get; set; } = string.Empty;

		public DateOnly EventDate { get; set; }

		public TimeOnly EventStartTime { get; set; }

		public TimeOnly EventEndTime { get; set; }

		public string Venue { get; set; } = string.Empty;

		public string Notes { get; set; } = string.Empty;

		public List<ParticipantDetailDto> Participants { get; set; } = new List<ParticipantDetailDto>();
	}
}


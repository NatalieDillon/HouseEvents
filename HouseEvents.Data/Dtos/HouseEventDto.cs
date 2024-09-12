using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseEvents.Data.Dtos
{
	public class HouseEventDto
	{
		public string HouseName { get; init; }

		public int Points { get; init; }

		public List<ParticipantDto> Participants { get; init; }

		public HouseEventDto(string houseName, int points) { 
			HouseName = houseName;
			Points = points;
			Participants = new List<ParticipantDto>();			
		}
	}
}

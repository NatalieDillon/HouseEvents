using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseEvents.Data.Dtos
{
	public class ParticipantDetailDto
	{
		public bool Reserve { get; set; }

		public string AllowableYearGroups { get; set; } = string.Empty;

		public int NumberRequired { get; set; }
	}
}

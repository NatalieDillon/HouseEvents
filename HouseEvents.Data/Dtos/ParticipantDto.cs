using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseEvents.Data.Dtos
{
    public class ParticipantDto
    {
        public int EventParticipantId { get; init; }

        public string? StudentName { get; init; }

        public bool Reserve { get; init; }

        public string AllowableYearGroups { get; init; }

        public bool? NoShow { get; init; }

        public ParticipantDto(int eventParticipantId, string? studentName, bool reserve, 
            string allowableYearGroups, bool? noShow) 
        { 
            EventParticipantId = eventParticipantId;
            StudentName = studentName;
            Reserve = reserve;
            AllowableYearGroups = allowableYearGroups;
            NoShow = noShow;
        }
    }
}

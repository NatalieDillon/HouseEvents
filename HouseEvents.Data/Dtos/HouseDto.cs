namespace HouseEvents.Data.Dtos
{
    public class HouseDto
    {
        private string _surmasterFirstName ;
        public string HouseName { get; init; } = string.Empty;
        public string SurmasterName
        {
            get { return $"{_surmasterFirstName} {HouseName}"; }
        }
        public string? ActivitiesCoordinator { get; init; }

        public HouseDto(string houseName, string surmasterFirstName, string? activitiesCoordinator) { 
            HouseName = houseName ;
            _surmasterFirstName = surmasterFirstName ;
            ActivitiesCoordinator = activitiesCoordinator ;
        }
    }
}
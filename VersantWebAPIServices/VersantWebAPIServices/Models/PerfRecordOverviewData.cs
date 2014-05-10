using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using SystemArts.VersantRN.Data;

namespace VersantWebAPIServices.Models
{
    public class PerfRecordOverviewData
    {
        public int CompetencyCoreCompleted { get; set; }
        public int CompetencyCoreOffered { get; set; }
        public decimal CompetencyCorePerc { get; set; }

        public int CompetencyFacilityCompleted { get; set; }
        public int CompetencyFacilityOffered { get; set; }
        public decimal CompetencyFacilityPerc { get; set; }

        public int CompetencyHospitalSystemCompleted { get; set; }
        public int CompetencyHospitalSystemOffered { get; set; }
        public decimal CompetencyHospitalSystemPerc { get; set; }

        public int CompetencySpecialtyCompleted { get; set; }
        public int CompetencySpecialtyOffered { get; set; }
        public decimal CompetencySpecialtyPerc { get; set; }

        public int CompetencyUnitCompleted { get; set; }
        public int CompetencyUnitOffered { get; set; }
        public decimal CompetencyUnitPerc { get; set; }

        public int UserID { get; set; }

        public PerfRecordOverviewData(PerfRecordOverview data)
        {
            this.CompetencyCoreCompleted = data.CompetencyCoreCompleted;
            this.CompetencyCoreOffered = data.CompetencyCoreOffered;
            this.CompetencyCorePerc = data.CompetencyCorePerc;

            this.CompetencyFacilityCompleted = data.CompetencyFacilityCompleted;
            this.CompetencyFacilityOffered = data.CompetencyFacilityOffered;
            this.CompetencyFacilityPerc = data.CompetencyFacilityPerc;

            this.CompetencyHospitalSystemCompleted = data.CompetencyHospitalSystemCompleted;
            this.CompetencyHospitalSystemOffered = data.CompetencyHospitalSystemOffered;
            this.CompetencyHospitalSystemPerc = data.CompetencyHospitalSystemPerc;

            this.CompetencySpecialtyCompleted = data.CompetencySpecialtyCompleted;
            this.CompetencySpecialtyOffered = data.CompetencySpecialtyOffered;
            this.CompetencySpecialtyPerc = data.CompetencySpecialtyPerc;

            this.CompetencyUnitCompleted = data.CompetencyUnitCompleted;
            this.CompetencyUnitOffered = data.CompetencyUnitOffered;
            this.CompetencyUnitPerc = data.CompetencyUnitPerc;

            this.UserID = data.UserID;
        }
    }
}
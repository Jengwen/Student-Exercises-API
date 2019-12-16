using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExercisesAPI.Models
{
    public class Student
    {
        public int Id { get; set; }
        [Required]
        public string FirstName { get; set; }
        [Required]
        [StringLength(25, MinimumLength=2)]

        public string LastName { get; set; }
        [Required]
        [StringLength(12, MinimumLength = 3)]
        public string SlackHandle { get; set; }
        public int CohortId { get; set; }
        public Cohort cohort { get; set; }

        public List<Exercise> assignedExercises { get; set; } = new List<Exercise>();
    }
}


using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace StudentExercisesAPI.Models
{
    public class Cohort
    {
        public int Id { get; set; }
        [Required]
        [StringLength(11, MinimumLength =5)]
        public string Name { get; set; }

        public List<Student> AssignedStudents { get; set; } = new List<Student>();
        public List<Instructor> CohortInstructors { get; set; } = new List<Instructor>();
    }
}

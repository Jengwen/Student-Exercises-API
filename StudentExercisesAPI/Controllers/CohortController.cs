using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Data;
using System.Data.SqlClient;
using StudentExercisesAPI.Models;
using Microsoft.AspNetCore.Http;

namespace StudentExercisesAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CohortsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CohortsController(IConfiguration config)
        {
            _config = config;
        }

        public SqlConnection Connection
        {
            get
            {
                return new SqlConnection(_config.GetConnectionString("DefaultConnection"));
            }
        }

        [HttpGet]
        public async Task<IActionResult> Get(string include, string q)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                    //SQL command to bring back all of the items needed
                {
                    cmd.CommandText = @"SELECT Cohort.Id, Cohort.Name, Student.Id AS 'Student Id', Student.FirstName,
Student.LastName, Student.SlackHandle, 
 Instructor.Id AS 'Instructor Id', Instructor.FirstName AS 'Instructor First Name',
Instructor.LastName AS 'Instructor Last Name', Instructor.SlackHandle AS 'Instructor Slack Handle', 
Instructor.CohortId AS 'Instructor Cohort Id' 
FROM Cohort  JOIN Student ON Student.CohortId = Cohort.Id JOIN Instructor ON Instructor.CohortId = Cohort.Id" ;
                    //query for Last_Name, First_Name, 
                    //if (q != null)
                    //{
                    //    query = @"";
                    //}

                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Cohort> cohorts = new List<Cohort>();

                    while (reader.Read())
                    {

                        //create individual instance of chort
                        Cohort currentCohort = new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                        };
                        //create individual instane of student
                        Student currentStudent = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle"))
                        };
                        //create single instance of instructor
                        Instructor currentInstructor = new Instructor
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Instructor Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("Instructor First Name")),
                            LastName = reader.GetString(reader.GetOrdinal("Instructor Last Name")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("Instructor Slack Handle"))
                        };
                        //If the cohorts list already has the current student in it, don't add it again
                        if (cohorts.Any(cohort => cohort.Id == currentCohort.Id))
                        {
                            //Find the cohort in the list if it's already there
                            Cohort cohortToReference = cohorts.Where(cohort => cohort.Id == currentCohort.Id).FirstOrDefault();

                            //Does the student already exist in the list
                            if (!cohortToReference.AssignedStudents.Any(student => student.Id == currentStudent.Id))

                            { cohortToReference.AssignedStudents.Add(currentStudent); }

                            //Does the instructor already exit in the list 
                            if (!cohortToReference.CohortInstructors.Any(instructor => instructor.Id == currentInstructor.Id))

                            { cohortToReference.CohortInstructors.Add(currentInstructor); }
                        }
                        else
                        {//Add cohorts to cohort list
                            // Add each student to each assigned cohort
                            currentCohort.AssignedStudents.Add(currentStudent);
                            //add each instructor to the cohort list
                            currentCohort.CohortInstructors.Add(currentInstructor);
                            cohorts.Add(currentCohort);
                        }
                                    }
                
                reader.Close();
//return list of cohorts
                return Ok(cohorts);
            }  
            }
        }

        [HttpGet("{id}", Name = "GetCohort")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, Name
                        FROM Cohort
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Cohort cohort = null;

                    if (reader.Read())
                    {
                        cohort = new Cohort
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                        };
                    }
                    reader.Close();

                    return Ok(cohort);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Cohort cohort)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Cohort (Name)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name)";
                    cmd.Parameters.Add(new SqlParameter("@name", cohort.Name));

                    int newId = (int)cmd.ExecuteScalar();
                    cohort.Id = newId;
                    return CreatedAtRoute("GetCohort", new { id = newId }, cohort);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Cohort cohort)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Cohort
                                            SET Name = @name
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", cohort.Name));
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!CohortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> Delete([FromRoute] int id)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"DELETE FROM Cohort WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@id", id));

                        int rowsAffected = cmd.ExecuteNonQuery();
                        if (rowsAffected > 0)
                        {
                            return new StatusCodeResult(StatusCodes.Status204NoContent);
                        }
                        throw new Exception("No rows affected");
                    }
                }
            }
            catch (Exception)
            {
                if (!CohortExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool CohortExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name
                        FROM Cohort
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}



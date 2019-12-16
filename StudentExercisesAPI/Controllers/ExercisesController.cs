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
    public class ExercisesController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ExercisesController(IConfiguration config)
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
        public async Task<IActionResult> Get(string include)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    string query = "SELECT Exercise.Id AS 'Exercise Id', Exercise.Name AS 'Exercise Name', Exercise.Language AS 'Exercise Language' FROM Exercise";
                    if (include == "students")
                    {
                        query = @"SELECT Exercise.Id AS 'Exercise Id', Exercise.Name AS 'Exercise Name', Exercise.Language AS 'Exercise Language',
Student.Id AS 'Student Id', Student.FirstName AS 'First Name', Student.LastName AS 'Last Name' FROM Exercise 
JOIN StudentExercise ON Exercise.Id = StudentExercise.ExerciseId JOIN Student ON StudentExercise.StudentId = Student.Id";
                    }

                    cmd.CommandText = query;
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Exercise> exercises = new List<Exercise>();

                    while (reader.Read())
                    {
                        
                        Exercise currentExercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Exercise Id")),
                            Name = reader.GetString(reader.GetOrdinal("Exercise Name")),
                            Language = reader.GetString(reader.GetOrdinal("Exercise Language"))
                        };
                       

                        //if query include = students then run second query to retrieve students assigned information back
                        if(include == "students")
                        {
                            Student currentStudent = new Student
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                                FirstName = reader.GetString(reader.GetOrdinal("First Name")),
                                LastName = reader.GetString(reader.GetOrdinal("Last Name"))
                            };

                            //if exercise is on the list do not add again
                            if (exercises.Any(exercise => exercise.Id == currentExercise.Id))

                            {
                                Exercise thisExercise = exercises.Where(e => e.Id == currentExercise.Id).FirstOrDefault();
                                thisExercise.assignedStudents.Add(currentStudent);
                            }
                            else

                            {
                                currentExercise.assignedStudents.Add(currentStudent);
                                exercises.Add(currentExercise);
                            }
                        }
                        else
                        {
                            //add exercises to list of exercises
                            exercises.Add(currentExercise);
                        }
                                          
                    }
                    reader.Close();

                    return Ok(exercises);
                }
            }
        }

        [HttpGet("{id}", Name = "GetExercise")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT
                            Id, Name, Language
                        FROM Exercise
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                   Exercise exercise = null;

                    if (reader.Read())
                    {
                        exercise = new Exercise
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            Name = reader.GetString(reader.GetOrdinal("Name")),
                            Language = reader.GetString(reader.GetOrdinal("Language"))
                        };
                    }
                    reader.Close();

                    return Ok(exercise);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Exercise exercise)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Exercise (Name, Language)
                                        OUTPUT INSERTED.Id
                                        VALUES (@name, @language)";
                    cmd.Parameters.Add(new SqlParameter("@name", exercise.Name));
                    cmd.Parameters.Add(new SqlParameter("@language", exercise.Language));

                    int newId = (int)cmd.ExecuteScalar();
                    exercise.Id = newId;
                    return CreatedAtRoute("GetExercise", new { id = newId }, exercise);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Exercise exercise)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Exercise
                                            SET Name = @name,
                                                Language = @language
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@name", exercise.Name));
                        cmd.Parameters.Add(new SqlParameter("@language", exercise.Language));
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
                if (!ExerciseExists(id))
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
                        cmd.CommandText = @"DELETE FROM Exercise WHERE Id = @id";
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
                if (!ExerciseExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool ExerciseExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, Name, Language
                        FROM Exercise
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
    }
}


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
    public class StudentsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public StudentsController(IConfiguration config)
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
                {

                    string query = @"SELECT Student.Id AS 'Student Id', Student.FirstName, Student.LastName, Student.SlackHandle,
Student.CohortId, Cohort.Name AS 'Cohort Name', Cohort.Id AS 'Cohort Id'
 FROM Student JOIN Cohort ON Student.CohortId = Cohort.Id";

                    if (include == "exercises")
                    {
                        query = @"SELECT Cohort.Id AS 'Cohort Id', Cohort.Name AS 'Cohort Name', Student.Id AS 'Student Id', Student.FirstName,
Student.LastName, Student.CohortId, Student.SlackHandle, Exercise.Id AS 'Exercise Id', Exercise.Name AS 'Exercise Name', Exercise.Language AS 'Language' FROM Cohort 
JOIN Student ON Student.CohortId = Cohort.Id JOIN StudentExercise ON Student.Id = StudentExercise.StudentId LEFT JOIN Exercise 
ON StudentExercise.ExerciseId = Exercise.Id";
                    }
                    //query for Last_Name, First_Name, 
                    if (q != null)
                    {
                        query = @"SELECT Id, FirstName, LastName, SlackHandle from Student WHERE LastName Like {q} OR FirstName LIKE {q} OR SlackHandle '{q}%'";
                    }

                    cmd.CommandText = query;
                    SqlDataReader reader = cmd.ExecuteReader();
                    List<Student> students = new List<Student>();

                    while (reader.Read())
                    {

                        Student currentStudent = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Student Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                            cohort = new Cohort
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Cohort Id")),
                                Name = reader.GetString(reader.GetOrdinal("Cohort Name"))
                            }
                        };

                        students.Add(currentStudent);
                        //if paramater includes exercise then add exercise to the query results and student list
                        if (include == "exercises")
                        {
                            Exercise currentExercise = new Exercise
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Exercise Id")),
                                Name = reader.GetString(reader.GetOrdinal("Exercise Name")),
                                Language = reader.GetString(reader.GetOrdinal("Language"))
                            };


                            //if student is on the list do not add again
                            if (students.Any(student => student.Id == currentStudent.Id))

                            {
                                Student thisStudent = students.Where(s => s.Id == currentStudent.Id).FirstOrDefault();
                                thisStudent.assignedExercises.Add(currentExercise);
                            }

                            else

                            {
                                currentStudent.assignedExercises.Add(currentExercise);
                                students.Add(currentStudent);
                            }
                        }
                        
                    }



                    reader.Close();

                    return Ok(students);
                }
            }
        }

        [HttpGet("{id}", Name = "GetStudent")]
        public async Task<IActionResult> Get([FromRoute] int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"SELECT Student.Id, Student.FirstName, Student.LastName, Student.SlackHandle,
Student.CohortId, Cohort.Name, Cohort.Id FROM Student JOIN Cohort ON Student.CohortId = Cohort.Id

                        WHERE Student.Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));
                    SqlDataReader reader = cmd.ExecuteReader();

                    Student student = null;

                    if (reader.Read())
                    {
                        student = new Student
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                            LastName = reader.GetString(reader.GetOrdinal("LastName")),
                            CohortId = reader.GetInt32(reader.GetOrdinal("CohortId")),
                            SlackHandle = reader.GetString(reader.GetOrdinal("SlackHandle")),
                            cohort = new Cohort
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                Name = reader.GetString(reader.GetOrdinal("Name"))
                            }
                        };
                       
                    }
                    reader.Close();

                    return Ok(student);
                }
            }
        }

        [HttpPost]
        public async Task<IActionResult> Post([FromBody] Student student)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"INSERT INTO Student (FirstName, LastName, SlackHandle, CohortId)
                                        OUTPUT INSERTED.Id
                                        VALUES (@FirstName, @LastName, @SlackHandle, @CohortId)";
                    cmd.Parameters.Add(new SqlParameter("@Firstname", student.FirstName));
                    cmd.Parameters.Add(new SqlParameter("@LastName", student.LastName));
                    cmd.Parameters.Add(new SqlParameter("@SlackHandle", student.SlackHandle));
                    cmd.Parameters.Add(new SqlParameter("@CohortId", student.CohortId));

                    int newId = (int)cmd.ExecuteScalar();
                    student.Id = newId;
                    return CreatedAtRoute("GetStudent", new { id = newId }, student);
                }
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Put([FromRoute] int id, [FromBody] Student student)
        {
            try
            {
                using (SqlConnection conn = Connection)
                {
                    conn.Open();
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandText = @"UPDATE Student
                                            SET FirstName = @FirstName,
                                                LastName = @LastName,
                                                SlackHandle = @SlackHandle,
                                                CohortId = @CohortId
                                            WHERE Id = @id";
                        cmd.Parameters.Add(new SqlParameter("@FirstName", student.FirstName));
                        cmd.Parameters.Add(new SqlParameter("@LastName", student.LastName));
                        cmd.Parameters.Add(new SqlParameter("@SlackHandle", student.SlackHandle));
                        cmd.Parameters.Add(new SqlParameter("@cohortId", student.CohortId));
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
                if (!StudentExists(id))
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
                        cmd.CommandText = @"DELETE FROM Student WHERE Id = @id";
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
                if (!StudentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
        }

        private bool StudentExists(int id)
        {
            using (SqlConnection conn = Connection)
            {
                conn.Open();
                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        SELECT Id, FirstName, LastName, SlackHandle, CohortId
                        FROM Student
                        WHERE Id = @id";
                    cmd.Parameters.Add(new SqlParameter("@id", id));

                    SqlDataReader reader = cmd.ExecuteReader();
                    return reader.Read();
                }
            }
        }
        
                        
                 
        }
    }



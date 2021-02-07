using System;
using System.Collections.Generic;
using System.Linq;
using GrooprWASM.Model;

namespace GrooprWASM.Data.Settings
{
    public class ResultValidator
    {
        public void ValidateSettings(List<Student> students)
        {
            List<string> studentNumbers = new List<string>();
            int idx = 1;
            
            ExtractStudentNumbers(students, idx, studentNumbers);

            CheckForDuplicates(students);
            
            ValidatePlusWishesTargets(students, studentNumbers);

            ValidateMinusWishesTargets(students, studentNumbers);
        }

        private void CheckForDuplicates(List<Student> students)
        {
            foreach (Student student in students)
            {
                if (!string.IsNullOrEmpty(student.PlusWishes))
                {
                    string[] plus = student.PlusWishes.Split("\n");
                    
                    if (plus.Contains(student.StudNum)) throw new Exception($"Student {student.StudNum} has wished themself");
                    var hashSet = new HashSet<string>();
                    foreach(var x in plus) 
                    {
                        if (!hashSet.Add(x))
                        {
                            throw new Exception($"Student {student.StudNum} has duplicate in plus wishes");
                        }
                    }
                }
                
                if (!string.IsNullOrEmpty(student.MinusWishes))
                {
                    string[] minus = student.MinusWishes.Split("\n");
                    if (minus.Contains(student.StudNum)) throw new Exception($"Student {student.StudNum} has wished themself");
                    var hashSet = new HashSet<string>();
                    foreach(var x in minus) 
                    {
                        if (!hashSet.Add(x))
                        {
                            throw new Exception($"Student {student.StudNum} has duplicate in minus wishes");
                        }
                    }
                }
            }
        }

        private static void ValidateMinusWishesTargets(List<Student> students, List<string> studentNumbers)
        {
            foreach (Student student in students)
            {
                if (student.MinusWishes != null)
                {
                    foreach (string num in student.MinusWishes.Split("\n"))
                    {
                        if (string.IsNullOrEmpty(num.Trim())) continue;
                        if (!studentNumbers.Contains(num.Trim()))
                        {
                            throw new Exception(
                                $"Student {student.StudNum} has minus number that cannot be found: {num}");
                        }
                    }
                }
            }
        }

        private static void ValidatePlusWishesTargets(List<Student> students, List<string> studentNumbers)
        {
            foreach (Student student in students)
            {
                if (student.PlusWishes != null)
                {
                    foreach (string num in student.PlusWishes.Split("\n"))
                    {
                        if (string.IsNullOrEmpty(num)) continue;
                        if (!studentNumbers.Contains(num))
                        {
                            throw new Exception(
                                $"Student {student.StudNum} has plus number that cannot be found: {num}");
                        }
                    }
                }
            }
        }

        private static void ExtractStudentNumbers(List<Student> students, int idx, List<string> studentNumbers)
        {
            foreach (Student student in students)
            {
                if (string.IsNullOrEmpty(student.StudNum))
                {
                    throw new Exception($"Student card {idx} is empty");
                }

                studentNumbers.Add(student.StudNum.Trim());
                idx++;
            }
        }
    }
}
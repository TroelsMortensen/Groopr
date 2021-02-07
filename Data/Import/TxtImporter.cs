using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using GrooprWASM.Data.Settings;
using GrooprWASM.Model;
using Microsoft.AspNetCore.Components.Forms;

namespace GrooprWASM.Data.Import
{
    public class TxtImporter
    {
        public async Task ImportFile(InputFileChangeEventArgs e, IBrowserFile file, SettingsContainer sc)
            {
                if (!file.Name.EndsWith("txt"))
                    throw new Exception("File format must be txt");
                string fromFile = await ReadStringFromFile(e);
                List<Student> students = CreateStudents(fromFile);
                sc.Students = students;
            }
        
            private List<Student> CreateStudents(string fromFile)
            {
                string[] studentsAsString = fromFile.Split("#");
                List<Student> students = new();
                foreach (string sas in studentsAsString)
                {
                    if (string.IsNullOrEmpty(sas)) continue;
                    Student student = new();
                    if (!sas.Contains(";")) // no wishes
                    {
                        student.StudNum = sas;
                        students.Add(student);
                        continue;
                    }
        
                    string[] s1 = sas.Split(";");
                    student.StudNum = s1[0].Trim();
        
                    if (s1[1].Length > 0)
                    {
                        student.PlusWishes = s1[1].Replace(",", "\n");
                    }
                    if (s1[2].Length > 0)
                    {
                        student.MinusWishes = s1[2].Replace(",", "\n");
                    }
                    students.Add(student);
                }
        
                return students;
            }
        
            private static async Task<string> ReadStringFromFile(InputFileChangeEventArgs e)
            {
                IBrowserFile file = e.File;
                long fileSize = file.Size;
                byte[] buffer = new byte[fileSize];
                await file.OpenReadStream().ReadAsync(buffer);
                var str = Encoding.Default.GetString(buffer);
                return str.Trim().Trim(new char[] {'\uFEFF'});
            }
    }
}
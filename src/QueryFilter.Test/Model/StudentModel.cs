using System;

namespace QueryFilter.Test.Model
{
    public class StudentModel
    {
        public string Name { get; set; }

        public string LastName { get; set; }

        public short Age { get; set; }

        public object NullValue { get; set; }

        public DateTime? Birth { get; set; }

        public DateTime Start { get; set; }


        public int? Total { get; set; }


    }
}
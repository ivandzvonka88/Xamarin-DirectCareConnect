using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC.Models.Questions
{
 
    public class QuestionList : ViewModelBase
    {
        public List<Question> questions { get; set; }
        public List<Option> valueIdOptions { get; set; }
        public Er er = new Er();
    }

    public class Question 
    {
        public int questionId { get; set; }

        public string valueType { get; set; }
        public int valueTypeId { get; set; }
        public string title { get; set; }
        public string minValue { get; set; }
        public string maxValue { get; set; }
        public string isActiveStr { get; set; }

        public bool  isActive{get;set;}

    

        public Er er = new Er();
    }
    public class OptionList : ViewModelBase
    {
        public List<Option> ValueIdOptions { get; set; }
        public Er er = new Er();
    }

    public class Option
    {
        public int value { get; set; }
        public string name { get; set; }

    }
}
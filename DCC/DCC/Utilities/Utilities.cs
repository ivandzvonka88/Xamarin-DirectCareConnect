using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace DCC
{
    public static class Utilities
    {
       

        public static string GenerateCode(int Len)
        {

            int cnt = 0;
            Random r = new Random();
            string nPass = "";
            while (cnt < Len)
            {
                int j = r.Next(48, 122);
                if ((j >= 48 && j <= 57) || (j >= 65 && j <= 90) || (j >= 97 && j <= 122))
                {
                    nPass = nPass + Convert.ToChar(j);
                    cnt = cnt + 1;
                }
            }
            return nPass;
        }



    }
}
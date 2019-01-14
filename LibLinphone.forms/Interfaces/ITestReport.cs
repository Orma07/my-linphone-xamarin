using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LibLinphone.forms.Interfaces
{
    public interface ITestReport
    {
        void SaveLog(int id);
        Task TakeScreen(int id);
    }
}

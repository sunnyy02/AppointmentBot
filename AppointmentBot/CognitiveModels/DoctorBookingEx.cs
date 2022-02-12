

using System.Linq;

namespace AppointmentBot.CognitiveModels
{
    // Extends the partial DoctorBooking class with methods and properties that simplify accessing entities in the luis results
    public partial class DoctorBooking
    {

        public string Doctor
        {
            get
            {
                var doctorChoosen = Entities?._instance?.Doctor?.FirstOrDefault()?.Text;
                return doctorChoosen;
            }
        }

        // This value will be a TIMEX. And we are only interested in a Date so grab the first result and drop the Time part.
        // TIMEX is a format that represents DateTime expressions that include some ambiguity. e.g. missing a Year.
        public string AppointmentDate
            => Entities.datetime?.FirstOrDefault()?.Expressions.FirstOrDefault()?.Split('T')[0];
    }
}

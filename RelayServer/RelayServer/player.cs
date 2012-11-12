using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace RelayServer
{
    class player
    {
        double id;
        public double xPosition;
        public double yPosition;

        public player(double id)
        {
            this.id = id;
            xPosition = 0;
            yPosition = 0;
        }
    }



}

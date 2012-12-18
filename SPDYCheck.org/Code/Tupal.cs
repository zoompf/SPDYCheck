using System;
using System.Collections.Generic;
using System.Text;

namespace Zoompf.General
{
    //Generic Tupal class;
    public class Tupal<I, J>
    {
        public I one;
        public J two;

        public Tupal()
        {
            this.one = default(I);
            this.two = default(J);
        }

        public Tupal(I i, J j)
        {
            this.one = i;
            this.two = j;
        }

    }
}

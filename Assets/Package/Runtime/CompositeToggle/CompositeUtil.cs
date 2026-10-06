using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Mobcast.Coffee.Toggles
{
    public class CompositeUtil
    {
        public static int[] CalculationState(int state, int count)
        {
            if (count == 0)
            {
                return new int[]{};
            }

            int[] states = new int[count]; ;
            //Nothing 
            if (state == 0)
            {
                for (int i = 0; i < count; i++)
                {
                    states[i] = 0;
                }
                return states;
            }

            //Everything
            if (state == -1)
            {
                for (int i = 0; i < count; i++)
                {
                    states[i] = 1;
                }
                return states;
            }
                
            //Mix
            string ret = Convert.ToString(state, 2);
            int index = ret.Length - 1;
            for (int i = 0; i < count; i++)
            {
                if (i >= ret.Length)
                {
                    states[i] = 0;
                }
                else
                {
                    if (index < ret.Length && index >= 0)
                    {
                        states[i] = int.Parse(ret[index].ToString());    
                    }
                    index--;
                }
            }
            
            return states;
        }
    }
}

Tool for generating hanging cables in 3D in realtime. Adapted from: https://www.alanzucconi.com/2020/12/13/catenary-1/

Variables:  
-Additional slack is how long the cable is beyond the minimum distance between the points. If the distance between the points is 20, and additional slack is 5, the resulting cable will be of length 25.

To do:  
-Use lofting to generate a mesh (rope, chain, etc.) along the line.  
-Add some basic transformations to emulate different environments such as having a slight back-and-forth horizontal wiggle to mimic a cable in the wind.

Bugs/Notes:  
-Since A (the variable) is approximated, there may be times where the cable goes slightly beyond the endpoint at a different angle (at least that's what I think is causing it lol).  
-This can be reduced by increasing the precision of A's estimation in the calculation function.

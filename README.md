# Beat Detection
This is a Unity program that attempts to find the beats present in an audio file using FFT's and variance. The main scene is BeatDetection and 
a utility scene Visualizer is present that can be used to see how the frequencies of given audio clip are distributed.

# Installation
This project has been made using Unity 2018.3. Clone the repo and open the folder using Unity Hub.

# Working
The program saves the average values of all the frequencies and then creates a dynamic threshold which when crossed changes the color of the 
respective cube.

# Current Progress
The color changes whenever the frequency crosses the dynamic threshold but the exact mapping of the frequencies to the numerical values set in 
script are to be explored. 

# TODO
    * Frequency window for different ranges (bass, mid, treble etc)
    * Realtime audio listening

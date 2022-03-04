using System.Collections;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class BeatDetector : MonoBehaviour {

    public AudioSource  audioSource;            // 
    public AudioClip    startingAudioClip;
    public Renderer     bassObjectRenderer;     // 
    public Color        bassColOld;             // 
    public Color        bassColNew;             // 
    public Material     bassObjectMaterial;     // 
                                                 
    public Renderer     lowObjectRenderer;      // 
    public Color        lowColOld;              // 
    public Color        lowColNew;              // 
    public Material     lowObjectMaterial;      // 
                                                 
    public int          bassLowerLimit = 60;    // 
    public int          bassUpperLimit = 180;   // 
    public int          lowLowerLimit = 500;    // 
    public int          lowUpperLimit = 2000;   // 
                                                 
    const float         lerp = 0.1f;            // 

    private int         windowSize;
    private float       samplingFrequency;

    float[]             freqSpectrum = new float[4];
    float[]             freqAvgSpectrum = new float[4];

    public bool         bass, low;

    Deque<List<float>>  FFTHistory_beatDetector = new Deque<List<float>>();

    int                 FFTHistory_maxSize;
    List<int>           beatDetector_bandLimits = new List<int>();

    void Awake() {
        // initialize
        audioSource.clip = startingAudioClip;
        audioSource.Play();
        int bandsize = audioSource.clip.frequency / 1024; // bandsize = (samplingFrequency / windowSize)

        FFTHistory_maxSize = audioSource.clip.frequency / 1024;

        beatDetector_bandLimits.Clear();

        //bass 60hz-180hz
        beatDetector_bandLimits.Add(bassLowerLimit / bandsize);
        beatDetector_bandLimits.Add(bassUpperLimit / bandsize);
        //low midrange 500hz-2000hz
        beatDetector_bandLimits.Add(lowLowerLimit / bandsize);
        beatDetector_bandLimits.Add(lowUpperLimit / bandsize);

        beatDetector_bandLimits.TrimExcess();
        FFTHistory_beatDetector.Clear();
    }

    // Update is called once per frame
    void Update() {
        // restart track by pressing space
        if (Input.GetKeyDown(KeyCode.Space)) {
            audioSource.Stop();
            audioSource.Play();
        }

        // stop music by pressing S
        if (Input.GetKeyDown(KeyCode.S)) {
            audioSource.Stop();
        }

        // Check if current sample are above statistical threshold
        GetBeat(ref freqSpectrum, ref freqAvgSpectrum, ref bass, ref low);

    }

    private void LateUpdate() {
        // change color of cubes based on booleans
        if (bass) {
            bassObjectMaterial.color = Color.Lerp(bassObjectMaterial.color, bassColNew, lerp);
        } else {
            bassObjectMaterial.color = Color.Lerp(bassObjectMaterial.color, bassColOld, lerp);
        }

        if (low) {
            lowObjectMaterial.color = Color.Lerp(lowObjectMaterial.color, lowColNew, lerp);
        } else {
            lowObjectMaterial.color = Color.Lerp(lowObjectMaterial.color, lowColOld, lerp);
        }
    }

    /// <summary>
    /// A function to set the booleans for beats by comparing current audio sample with statistical values of previous one's
    /// </summary>
    /// <param name="spectrum">reference to the array containing current samples and amplitudes</param>
    /// <param name="avgSpectrum">reference to the array containing average values for the sample amplitudes</param>
    /// <param name="isBass">bool to check if current value is higher than average for bass frequencies</param>
    /// <param name="isLow">bool to check if current value is higher than average for low-mid frequencies</param>
    void GetBeat (ref float[] spectrum, ref float[] avgSpectrum, ref bool isBass, ref bool isLow) {

        int numBands = 2; //beatDetector_bandLimits.size() / 2 
        int numChannels = audioSource.clip.channels;
        for (int numBand = 0; numBand < numBands; ++numBand) {
            for (int indexFFT = beatDetector_bandLimits[numBand]; indexFFT < beatDetector_bandLimits[numBand + 1]; ++indexFFT) {
                for (int channel = 0; channel < numChannels; ++channel) {
                    float[] tempSample = new float[1024];
                    audioSource.GetSpectrumData(tempSample, channel, FFTWindow.Rectangular);
                    spectrum[numBand] += tempSample[indexFFT];
                }
            }
            spectrum[numBand] /= (beatDetector_bandLimits[numBand + 1] - beatDetector_bandLimits[numBand] * numBand);
        }
        if (FFTHistory_beatDetector.Count > 0) {
            FillAvgSpectrum(ref avgSpectrum, numBands, ref FFTHistory_beatDetector);

            float[] varianceSpectrum = new float[numBands];

            FillVarianceSpectrum(ref varianceSpectrum, numBands, ref FFTHistory_beatDetector, ref avgSpectrum);
            isBass = (spectrum[0]) > BeatThreshold(varianceSpectrum[0]) * avgSpectrum[0];
            isLow = (spectrum[1]) > BeatThreshold(varianceSpectrum[1]) * avgSpectrum[1];
        }

        List<float> fftResult = new List<float>(numBands);

        for (int index = 0; index < numBands; ++index) {
            fftResult.Add(spectrum[index]);
        }

        if (FFTHistory_beatDetector.Count >= FFTHistory_maxSize) {
            FFTHistory_beatDetector.RemoveFront();
        }
        FFTHistory_beatDetector.AddBack(fftResult);
    }

    /// <summary>
    /// Function to add average values to the array
    /// </summary>
    /// <param name="avgSpectrum"></param>
    /// <param name="numBands"></param>
    /// <param name="fftHistory"></param>
    void FillAvgSpectrum(ref float[] avgSpectrum, int numBands, ref Deque<List<float>> fftHistory) {
        foreach (List<float> iterator in fftHistory) {
            List<float> fftResult = iterator;

            for (int index = 0; index < fftResult.Count; ++index) {
                avgSpectrum[index] += fftResult[index];
            }
        }

        for (int index = 0; index < numBands; ++index) {
            avgSpectrum[index] /= (fftHistory.Count);
        }
    }

    /// <summary>
    /// Function to add variance values to the array
    /// </summary>
    /// <param name="varianceSpectrum"></param>
    /// <param name="numBands"></param>
    /// <param name="fftHistory"></param>
    /// <param name="avgSpectrum"></param>
    void FillVarianceSpectrum(ref float[] varianceSpectrum, int numBands, ref Deque<List<float>> fftHistory, ref float[] avgSpectrum) {
        foreach (List<float> iterator in fftHistory) {
            List<float> fftResult = iterator;

            for (int index = 0; index < fftResult.Count; ++index) {
                //Debug.Log("fftresult val is - " + fftResult[index]);
                varianceSpectrum[index] += (fftResult[index] - avgSpectrum[index]) * (fftResult[index] - avgSpectrum[index]);
            }
        }

        for (int index = 0; index < numBands; ++index) {
            varianceSpectrum[index] /= (fftHistory.Count);
        }
    }

    /// <summary>
    /// Function to get the threshold value for the sample
    /// </summary>
    /// <param name="variance">variance for the sample</param>
    /// <returns>float threshold</returns>
    float BeatThreshold(float variance) {
        return -15f * variance + 1.55f;
    }

    /// <summary>
    /// Function to change audio clip based on UI buttons in the scene
    /// </summary>
    /// <param name="clip">the clip to play</param>
    public void ChangeClip(AudioClip clip) {
        if (audioSource.isPlaying) {
            audioSource.Pause();
        }
        audioSource.clip = clip;
        audioSource.Play();
    }
}
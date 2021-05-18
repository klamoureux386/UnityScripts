using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioController : MonoBehaviour
{

    public AudioSource audioSource;
    public AudioClip gunshot1;

    public void playGunshot() {

        //slight variation in volume
        audioSource.PlayOneShot(gunshot1, Random.Range(0.65f, 0.7f));

    }
}

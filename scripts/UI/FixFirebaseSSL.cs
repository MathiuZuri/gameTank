using UnityEngine;

public class FixFirebaseSSL : MonoBehaviour
{
    void Awake()
    {
        // Esto permite conexiones inseguras (SOLO PARA DEBUG)
        System.Net.ServicePointManager.ServerCertificateValidationCallback = 
            (sender, certificate, chain, sslPolicyErrors) => true;
    }
}
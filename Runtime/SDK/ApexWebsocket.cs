using UnityEngine;
using Unity.Networking.Transport;
using System.Net.WebSockets;

namespace PixoVR.Apex
{
    public class ApexWebsocket : MonoBehaviour
    {
        NetworkDriver Driver;

        private void Awake()
        {
            Driver = NetworkDriver.Create();
        }

        // Start is called before the first frame update
        void Start()
        {
        
        }

        // Update is called once per frame
        void Update()
        {
        
        }
    }
}

using System.Collections;
using UnityEngine;


public class OptimizedRenderer : MonoBehaviour
{
    private PlayerInputScript playerScript;

    private void Awake()
    {
        playerScript = GameObject.FindGameObjectWithTag("Bob").GetComponent<PlayerInputScript>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Light"))
        {
            other.gameObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>().enabled = true;
            other.gameObject.transform.GetChild(0).gameObject.SetActive(true);

            if (!playerScript.flashlightEnabled)
            {
                other.gameObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = 6f;
            }
            else if (playerScript.flashlightEnabled)
            {
                other.gameObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>().intensity = 1.5f;
            }
        }

        if (other.gameObject.CompareTag("Shadow"))
        {
            other.gameObject.GetComponent<UnityEngine.Rendering.Universal.ShadowCaster2D>().enabled = true;
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.CompareTag("Light"))
        {
            other.gameObject.GetComponent<UnityEngine.Rendering.Universal.Light2D>().enabled = false;
            other.gameObject.transform.GetChild(0).gameObject.SetActive(false);
        }

        if (other.gameObject.CompareTag("Shadow"))
        {
            other.gameObject.GetComponent<UnityEngine.Rendering.Universal.ShadowCaster2D>().enabled = false;
        }
    }
}

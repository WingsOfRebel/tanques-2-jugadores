using System;
using UnityEngine;

// Configura y controla un tanque. Cambios: intentos perdidos (m_Losses) y puntaje (m_Score).
[Serializable]
public class TankManager
{
    public Color m_PlayerColor;                         // Color del tanque
    public Transform m_SpawnPoint;                      // Dónde aparece
    [HideInInspector] public int m_PlayerNumber;        // Número de jugador
    [HideInInspector] public string m_ColoredPlayerText; // "JUGADOR 1" con su color
    [HideInInspector] public GameObject m_Instance;     // El tanque creado
    [HideInInspector] public int m_Wins;                // Rondas ganadas
    [HideInInspector] public int m_Losses;              // Intentos perdidos (RF-04)
    [HideInInspector] public int m_Score;               // Puntaje

    private TankMovement m_Movement;
    private TankShooting m_Shooting;
    private GameObject m_CanvasGameObject;              // Barras de salud/disparo

    public void Setup()
    {
        m_Movement = m_Instance.GetComponent<TankMovement>();
        m_Shooting = m_Instance.GetComponent<TankShooting>();
        m_CanvasGameObject = m_Instance.GetComponentInChildren<Canvas>().gameObject;

        m_Movement.m_PlayerNumber = m_PlayerNumber;
        m_Shooting.m_PlayerNumber = m_PlayerNumber;

        m_ColoredPlayerText = "<color=#" + ColorUtility.ToHtmlStringRGB(m_PlayerColor) + ">JUGADOR " + m_PlayerNumber + "</color>";

        // Pinta todas las partes del tanque con el color del jugador
        MeshRenderer[] renderers = m_Instance.GetComponentsInChildren<MeshRenderer>();
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.color = m_PlayerColor;
        }
    }

    // Fases en las que el jugador no controla el tanque
    public void DisableControl()
    {
        m_Movement.enabled = false;
        m_Shooting.enabled = false;
        m_CanvasGameObject.SetActive(false);
    }

    // Fases en las que el jugador sí controla el tanque
    public void EnableControl()
    {
        m_Movement.enabled = true;
        m_Shooting.enabled = true;
        m_CanvasGameObject.SetActive(true);
    }

    // Al inicio de cada ronda: tanque en su estado inicial
    public void Reset()
    {
        m_Instance.transform.position = m_SpawnPoint.position;
        m_Instance.transform.rotation = m_SpawnPoint.rotation;

        m_Instance.SetActive(false);
        m_Instance.SetActive(true);
    }

    // Al empezar una partida nueva
    public void ReiniciarMarcadores()
    {
        m_Wins = 0;
        m_Losses = 0;
        m_Score = 0;
    }
}

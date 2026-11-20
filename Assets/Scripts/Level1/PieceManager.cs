using NUnit.Framework;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine;
using UnityEngine.Jobs;

public class PieceManager : MonoBehaviour
{
    private GameObject[] pieceArray;

    private float startPosX = 0.5F;
    private float startPosZ = 0.6F;
    private float pieceDistanceX = 0.4F;
    private float pieceDistanceZ = -0.4F;

    private Vector3[] initializeTransforms()
    {
        Vector3[] transformArray = new Vector3[12];

        for (int i = 0; i < 4; i++)
            for (int j = 0; j < 3; j++)
            {
                transformArray[(3 * i) + j] = 
                    new Vector3(startPosX + j * pieceDistanceX, 1.7F, startPosZ + i * pieceDistanceZ);

                //Debug.Log("transformArray [" + (3 * i + j) + "] coordinate is (" +
                //    transformArray[3 * i + j].x + " | " + transformArray[3 * i + j].z + ")");
            }

        return transformArray;
    }
    private void shuffleTransforms(Vector3[] transformArray)
    {
        for (int t = 0; t < transformArray.Length; t++)
        {
            Vector3 tmp = transformArray[t];
            int r = Random.Range(t, transformArray.Length);
            transformArray[t] = transformArray[r];
            transformArray[r] = tmp;
        }
    }

    private void shuffleRotation(GameObject piece)
    {
        /*
        float randomX = (Random.Range(0, 3) * 90);
        float randomY = (Random.Range(0, 3) * 90);
        float randomZ = (Random.Range(0, 3) * 90);

        piece.transform.Rotate(randomX, randomY, randomZ);
        */

        float RandomY = (Random.Range(0, 3) * 90);

        Quaternion targetRotation = Quaternion.AngleAxis(RandomY, Vector3.up) * piece.transform.rotation;

        piece.transform.rotation = targetRotation;
    }

    private void Start()
    {
        pieceArray = GameObject.FindGameObjectsWithTag("Peca");
        Vector3[] spawnPositions = initializeTransforms();
        GameObject sternal = null;

        foreach (GameObject piece in pieceArray)
        {
            if (piece != null && piece.name == "Esternal")
            {
                sternal = piece;
                break;
            }
        }

        if (sternal == null || pieceArray.Length != spawnPositions.Length)
        {
            Debug.LogError("PieceManager: eram esperadas 12 peças com a peça Esternal.", this);
            return;
        }

        // Reserve sempre a primeira posição da grade para o Esternal. As demais
        // continuam sendo distribuídas aleatoriamente entre as outras peças.
        sternal.transform.position = spawnPositions[0];
        Move3D sternalMove = sternal.GetComponent<Move3D>();
        if (sternalMove != null)
            sternalMove.startPosition = spawnPositions[0];

        OutlineManager sternalOutline = sternal.GetComponent<OutlineManager>();
        if (sternalOutline != null && sternalOutline.correctSnap != null)
        {
            // Começa sempre 90 graus fora da rotação correta, exigindo que o
            // jogador use uma das setas durante o tutorial.
            sternal.transform.rotation = Quaternion.AngleAxis(90f, Vector3.up) *
                sternalOutline.correctSnap.transform.rotation;
        }
        else
        {
            Debug.LogError("PieceManager: o Esternal precisa ter OutlineManager com correctSnap configurado.", sternal);
            return;
        }

        Vector3[] otherPositions = new Vector3[spawnPositions.Length - 1];
        for (int i = 1; i < spawnPositions.Length; i++)
            otherPositions[i - 1] = spawnPositions[i];
        shuffleTransforms(otherPositions);

        int positionIndex = 0;
        foreach (GameObject piece in pieceArray)
        {
            if (piece == null || piece == sternal)
                continue;

            Vector3 position = otherPositions[positionIndex++];
            piece.transform.position = position;
            Move3D move = piece.GetComponent<Move3D>();
            if (move != null)
                move.startPosition = position;
            shuffleRotation(piece);
        }
    }
}

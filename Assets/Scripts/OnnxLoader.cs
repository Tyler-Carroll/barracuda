using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Barracuda;
using UnityEngine;
using UnityEngine.UI;

public class OnnxLoader : MonoBehaviour
{
    [Tooltip("Performs the preprocessing and postprocessing steps")]
    public ComputeShader styleTransferShader;

    [Tooltip("Stylize the camera feed")]
    public bool stylizeImage = true;

    [Tooltip("The height of the image being fed to the model")]
    public int targetHeight = 255;

    public NNModel modelAsset;

    private IWorker engine;

    public WorkerFactory.Type workerType = WorkerFactory.Type.Auto;

    private void Start()
    {
        var model = ModelLoader.Load(modelAsset);
        engine = WorkerFactory.CreateWorker(workerType, model);

    }

    private void ProcessImage(RenderTexture image, string functionName)
    {
        // Specify the number of threads on the GPU 
        int numthreads = 8;
        // Get the index for the specified function in the ComputeShader 
        int kernelHandle = styleTransferShader.FindKernel(functionName);
        // Define a temporary HDR RenderTexture
        RenderTexture result = RenderTexture.GetTemporary(image.width,
     image.height, 24, RenderTextureFormat.ARGBHalf);
        // Enable random write access
        result.enableRandomWrite = true;
        // Create the HDR RenderTexture
        result.Create();

        // Set the value for the Result variable in the ComputeShader 
        styleTransferShader.SetTexture(kernelHandle, "Result", result);
        // Set the value for the InputImage variable in the ComputeShader 
        styleTransferShader.SetTexture(kernelHandle, "InputImage",
     image);

        // Execute the ComputeShader
        styleTransferShader.Dispatch(kernelHandle, result.width /
     numthreads, result.height / numthreads, 1);
        // Copy the result into the source RenderTexture
        Graphics.Blit(result, image);
        // Release the temporaryRenderTexture
        RenderTexture.ReleaseTemporary(result);
    }

    /// <summary>
    /// Stylize the provided image
    /// </summary>
    /// <param name="src"></param>
    /// <returns></returns>
    private void StylizeImage(RenderTexture src)
    {
        // Create a new RenderTexture variable 
        RenderTexture rTex;

        // Check if the target display is larger than the targetHeightand make sure the targetHeight is at least 8
        if(src.height > targetHeight && targetHeight >= 8)
        {
            // Calculate the scale value for reducing the size of the input image
           float scale = src.height / targetHeight;
            // Calculate the new image width 
            int targetWidth = (int)(src.width / scale);

            // Adjust the target image dimensions to be multiples of 8 
            targetHeight -= (targetHeight % 8);
            targetWidth -= (targetWidth % 8);
            //Assign a temporary RenderTexture with the new dimensions
            rTex = RenderTexture.GetTemporary(targetWidth, targetHeight, 24, src.format);
        }
    else
        {
            // Assign a temporary RenderTexture with the src dimensions
            rTex = RenderTexture.GetTemporary(src.width, src.height, 24,
     src.format);
        }

        // Copy the src RenderTexture to the new rTex RenderTexture
        Graphics.Blit(src, rTex);

        // Apply preprocessing steps
        ProcessImage(rTex, "ProcessInput");

        // Create a Tensor of shape [1, rTex.height, rTex.width, 3]
        Tensor input = new Tensor(rTex, channels: 3);

        // Execute neural network with the provided input
        engine.Execute(input);

        // Get the raw model output
        Tensor prediction = engine.PeekOutput();
        // Release GPU resources allocated for the Tensor
        input.Dispose();

        // Make sure rTex is not the active RenderTexture
        RenderTexture.active = null;
        // Copy the model output to rTex
        prediction.ToRenderTexture(rTex);
        // Release GPU resources allocated for the Tensor
        prediction.Dispose();

        // Apply post processing steps
        ProcessImage(rTex, "ProcessOutput");
        // Copy rTex into src
        Graphics.Blit(rTex, src);

        // Release the temporary RenderTexture
        RenderTexture.ReleaseTemporary(rTex);
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest)
    {
        if (stylizeImage)
        {
            StylizeImage(src);
        }

        Graphics.Blit(src, dest);
    }

    private void OnDisable()
    {
        engine.Dispose();
    }
}

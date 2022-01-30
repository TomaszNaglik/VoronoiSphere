using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraController : MonoBehaviour
{
    
    float zoom = 1f;
    [Range(10.0f, 100.00f)]
    private Vector2 movementSpeed;
    public float maxMvtSpeed;
    public float minMvtSpeed;
    public float friction;
    public float maxZoomSpeed;
    public float minZoomSpeed;
    private float zoomSpeed;
    private float zoomDelta;


    private float scale = 100;

    public float stickMinZoom, stickMaxZoom;

    public Vector2 initialPosition = new Vector2(0, 0);
    private Vector2 polarPosition;
    private Vector3 cartesianPosition;


    Transform swivel, stick;

    private void Awake()
    {
        swivel = transform.GetChild(0);
        stick = swivel.GetChild(0);
    }
    void Start()
    {
        polarPosition = initialPosition;
        cartesianPosition = VParams.PolarToCartesian(polarPosition) * scale;
        
    }

    // Update is called once per frame
    void Update()
    {
        Move();
        Zoom();
        
        
    }

    void Move()
    {
        float HorizontalMovement = Input.GetAxis("Horizontal");
        float VerticalMovement = Input.GetAxis("Vertical");

        movementSpeed.x += (VerticalMovement * Time.deltaTime);
        movementSpeed.y += (HorizontalMovement * Time.deltaTime);

        if (movementSpeed.x > maxMvtSpeed) movementSpeed.x = maxMvtSpeed;
        if (movementSpeed.y > maxMvtSpeed) movementSpeed.y = maxMvtSpeed;

        if (movementSpeed.x < -maxMvtSpeed) movementSpeed.x = -maxMvtSpeed;
        if (movementSpeed.y < -maxMvtSpeed) movementSpeed.y = -maxMvtSpeed;
        polarPosition -= movementSpeed;
        movementSpeed *= friction;
        if (Math.Abs(movementSpeed.x) < minMvtSpeed) movementSpeed.x = 0;
        if (Math.Abs(movementSpeed.y) < minMvtSpeed) movementSpeed.y = 0;


        if (polarPosition.x > 89) polarPosition.x = 89;
        if (polarPosition.x < -89) polarPosition.x = -89;
        
        transform.up = transform.position;
        cartesianPosition = VParams.PolarToCartesian(polarPosition) * scale;
        transform.position = cartesianPosition;

        
        

    }

    void Zoom()
    {
        transform.up = transform.position.normalized;
        
        zoomSpeed += Input.GetAxis("Mouse ScrollWheel");
        zoom -= zoomSpeed;
        if (zoomSpeed > maxZoomSpeed) zoomSpeed = maxZoomSpeed;
        
        zoomSpeed *= friction;

        if (Math.Abs(zoomSpeed) < minZoomSpeed) zoomSpeed = 0;

        if (zoom < stickMinZoom) zoom = stickMinZoom;
        if (zoom > stickMaxZoom) zoom = stickMaxZoom;

        stick.localPosition = new Vector3(0f, 0f, -zoom);
        //Debug.LogFormat("Zoom: {0}, ZoomSpeed: {1}", zoom, zoomSpeed);
        /*if (zoomDelta != 0f)
        {
            AdjustZoom(zoomDelta);
            float distance = Mathf.Lerp(stickMinZoom, stickMaxZoom, zoom);
            stick.localPosition = new Vector3(0f, 0f, -distance);

        }*/

    }
    void AdjustZoom(float delta)
    {
        zoom = Mathf.Clamp01(zoom + delta);
    }
}

using UnityEngine;

public class NormalMapAnimator : MonoBehaviour
{
    public Vector2 speed = new Vector2(0.1f, 0.1f); // Скорость анимации по оси X и Y

    public Material material;
    public Vector2 offset;

    void Start()
    {
        // Получаем материал объекта. Убедитесь, что объект имеет Renderer.
        material = GetComponent<Renderer>().sharedMaterial;
    }

    void Update()
    {
        // Обновляем offset на основе скорости и времени
        offset += speed * Time.deltaTime;

        // Применяем новый offset к текстуре Normal Map
        material.SetTextureOffset("_MainTex", offset);
    }
}

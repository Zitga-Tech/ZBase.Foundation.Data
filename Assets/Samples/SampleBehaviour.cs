using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json.Serialization;
using UnityEngine;

public class SampleBehaviour : MonoBehaviour
{
    private void Start()
    {
        var snake = new SnakeCaseNamingStrategy();

        Debug.Log("Snake");
        Debug.Log(snake.GetPropertyName("AlterName", false));
        Debug.Log(snake.GetPropertyName("alter_name", false));
        Debug.Log(snake.GetPropertyName("alter-name", false));

        var camel = new CamelCaseNamingStrategy();

        Debug.Log("Camel");
        Debug.Log(camel.GetPropertyName("AlterName", false));
        Debug.Log(camel.GetPropertyName("alter_name", false));
        Debug.Log(camel.GetPropertyName("alter-name", false));

        var kebab = new KebabCaseNamingStrategy();

        Debug.Log("Kebab");
        Debug.Log(kebab.GetPropertyName("AlterName", false));
        Debug.Log(kebab.GetPropertyName("alter_name", false));
        Debug.Log(kebab.GetPropertyName("alter-name", false));
    }
}

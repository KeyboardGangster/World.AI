using OpenAI;
using OpenAI.Models;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PromptGenerator : MonoBehaviour
{

    public string inputField;

    private void Start()
    {
        Main();
    }

    public async void Main()
    {
        var api = new OpenAIClient("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");
        string prompt = "Given is an array of possible biomes to chose from with their descriptions: " +
            "[name: Desert(Vegetation), " +
            "description: Relatively flat desert with cacti and other dry vegetation.], " +
            "[name: Field(Dry)," +
            "description: Flat field full of tall dry grass and occasional boulders.], " +
            "[name: Field(Flowers), " +
            "description: Flat field full of flowers and ocasional boulders.], " +
            "[name: Field(Flowers, Pattern), " +
            "description: Flat field full of flowers growing in a curious pattern and ocasional boulders.], " +
            "[name: Field, " +
            "description: Flat field full of grass and occasional boulders.], " +
            "[name: Forest(Birch), " +
            "description: Old birchtree-forest on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Forest(Conifer), " +
            "description: Old coniferous forest on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Forest(Maple), " +
            "description: Mapletree-forest on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Forest(Mixed Trees), " +
            "description: mixed-trees - forest containing maple-, chestnut - and various other trees on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Forest(Young Birch), " +
            "description: Young birchtree-forest on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Forest(Young Maple), " +
            "description: Young Mapletree-forest on a relatively flat terrain with ocasional boulder formations.], " +
            "[name: Hill Forest(Conifer), " +
            "description: Old forest with coniferous trees on a hill.], " +
            "[name: Hill Forest(Young Conifer, Small Hills), " +
            "description: Young forest with coniferous trees on small hills.], " +
            "[name: Hills(Small), " +
            "description: Small grassy hills with lush grass and little other vegetation.], " +
            "[name: Hills, " +
            "description: Grassy hills with lush grass and little other vegetation.], " +
            "[name: Mountain Forest(Conifer), " +
            "description: Forest with coniferous trees on top of a mountain.], " +
            "[name: Mountains, " +
            "description: Mountainous terrain with dry grass and no trees.], " +
            "[name: Sanddunes(Large), " +
            "description: Desert full of large sanddunes several 10s of meters in size.], " +
            "[name: Sanddunes(Medium), " +
            "description: Desert full of sanddunes several of meters in size.], " +
            "[name: Sanddunes(Small), " +
            "description: Desert full of small sanddunes up to one meter in size.Seldomly, lone cacti can be found.], " +
            "[name: Savanna(Flooded), " +
            "description: Savanna near a pond with occasional trees, rocks and dry grass.], " +
            "[name: Savanna, " +
            "description: Savanna with occasional trees and rocks and dry grass.], " +
            "[name: Swamp(Dry), " +
            "description: A Swamp that is relatively dry with some trees.], " +
            "[name: Swamp(Wet), " +
            "description: A Swamp that is wet and muddy with trees.], " +
            "[name: Swamp, " +
            "description: A Swamp with trees.] " +
            "Based on the array above please pick what you would be the best biomes to use for the following prompt and please use following format for the answer: " +
            "Format: [Biome1], [Biome2], [Biome3], etc... " +
            $"Prompt: '{inputField}'";
        Debug.Log(prompt);
        var result = await api.CompletionsEndpoint.CreateCompletionAsync( prompt, temperature: 0.1, model: Model.Davinci);
        Debug.Log(result);
    }

}

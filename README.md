# Patches: AI-Driven Virtual Companion

## 🤖 Overview
An immersive VR companion project for Meta Quest 3 that explores the synergy between LLMs (GPT) and game logic. The project defines a character with dynamic personality shifts between "Companionship" and "Self-Entertainment" modes.

## 🛠 Key Responsibilities & Features
* **Multimodal Interaction Framework**: Built the voice-to-action and physical interaction modules, enabling users to converse with the character or engage in physical games like "Catch".
* **Behavior-Sentiment Mapping**: Developed a middleware to map GPT-generated prompts and emotional cues to specific in-game character animations and behaviors.
* **Autonomous Navigation & Agency**: Implemented a NavMesh-based autonomous behavior tree, allowing the character to proactively interact with the environment based on its current state.
* **Prompt-Logic Integration**: Designed a robust logic framework to translate natural language prompts into deterministic semantic mappings for character responses.

## 🧠 Engineering Insights 
* **Decoupling AI & Presentation**: Followed a modular approach to separate the LLM response handling from the character's animation controller, ensuring the game loop remains responsive regardless of API latency.
* **Agent Personality Design**: Used structured Prompt Engineering to define core character traits, ensuring consistent behavior across diverse user interaction scenarios.

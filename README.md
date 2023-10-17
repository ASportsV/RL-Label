# RL-LABEL: A Deep Reinforcement Learning Approach Intended for AR Label Placement in Dynamic Scenarios

> *Chen Zhu-Tian, Daniele Chiappalupi, Tica Lin, Yalong Yang, Johanna Beyer, Hanspeter Pfister*  
> *IEEE Transactions on Visualization and Computer Graphics (IEEE VIS), 2023*  
> [ [Paper](https://browse.arxiv.org/pdf/2308.13540.pdf) | [Video]() | [Training Code](https://github.com/ASportsV/vis-ml-agent)]


The Unity program for the paper *"RL-LABEL: A Deep Reinforcement Learning Approach Intended for AR Label Placement in Dynamic Scenarios"*

## **Prerequisites**
- Unity 2021.3.0 or later
- Unity XR Plugin Management (Install via Unity Package Manager)
- Compatible VR headset (e.g. Oculus Quest 2, HTC Vive)

## Citation

```
@ARTICLE {chen2023RL_Label,
    title={RL-LABEL: A Deep Reinforcement Learning Approach Intended for AR Label Placement in Dynamic Scenarios},
    author={Zhu-Tian, Chen and Chiappalupi, Daniele and Lin, Tica and Yang, Yalong and Beyer, Johanna and Pfister, Hanspeter},
    booktitle={IEEE Transactions on Visualization and Computer Graphics (IEEE VIS)},
    year={2023},
    month={Oct},
    publisher={IEEE}
}
```
## **Abstract**

Labels are widely used in augmented reality (AR) to display digital information. Ensuring the readability of AR labels requires placing them occlusion-free while keeping visual linkings legible, especially when multiple labels exist in the scene. Although existing optimization-based methods, such as force-based methods, are effective in managing AR labels in static scenarios, they often struggle in dynamic scenarios with constantly moving objects. This is due to their focus on generating layouts optimal for the current moment, neglecting future moments and leading to sub-optimal or unstable layouts over time. In this work, we present RL-LABEL, a deep reinforcement learning-based method for managing the placement of AR labels in scenarios involving moving objects. RL-LABEL considers the current and predicted future states of objects and labels, such as positions and velocities, as well as the user’s viewpoint, to make informed decisions about label placement. It balances the trade-offs between immediate and long-term objectives. Our experiments on two real-world datasets show that RL-LABEL effectively learns the decision-making process for long-term optimization, outperforming two baselines (i.e., no view management and a force-based method) by minimizing label occlusions, line intersections, and label movement distance. Additionally, a user study involving 18 participants indicates that RL-LABEL excels over the baselines in aiding users to identify, compare, and summarize data on AR labels within dynamic scenes.

### ToDo
- [ ] add more documentations

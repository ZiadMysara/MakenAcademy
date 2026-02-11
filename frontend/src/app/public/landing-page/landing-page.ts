import { Component } from '@angular/core';
import { HeroSection } from '../components/hero-section/hero-section';
import { OrganizationShowcase } from '../components/organization-showcase/organization-showcase';
import { FeaturesSection } from '../components/features-section/features-section';
import { HowItWorksSection } from '../components/how-it-works-section/how-it-works-section';

@Component({
  selector: 'app-landing-page',
  imports: [
    HeroSection,
    OrganizationShowcase,
    FeaturesSection,
    HowItWorksSection
  ],
  templateUrl: './landing-page.html',
  styleUrl: './landing-page.css',
})
export class LandingPage {

}
